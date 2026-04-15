using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Requests.Consultation;
using SnakeAid.Core.Requests.LiveKit;
using SnakeAid.Core.Responses.Consultation;
using SnakeAid.Core.Responses.LiveKit;
using SnakeAid.Core.Responses.PayOs;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Hubs;
using SnakeAid.Service.Implements;
using SnakeAid.Service.Interfaces;
using System.Linq.Expressions;

namespace SnakeAid.Tests.Unit;

public class RoomCleanupTests
{
    #region 8.1 — Order: ConsultationCallEnded signal + DeleteRoom BEFORE status update

    [Fact]
    public async Task AutoCompleteScheduled_SendsSignalAndDeletesRoom_BeforeStatusUpdate()
    {
        // Arrange
        var consultationId = Guid.NewGuid();
        var operationLog = new List<string>();

        var booking = MakeElapsedBooking(consultationId);
        var uow = new SpyUnitOfWork(
            bookings: new[] { booking },
            onCommit: () => operationLog.Add("commit"));
        var hub = new SpyHubContext(onSend: (group, method, args) => operationLog.Add($"signal:{method}"));
        var liveKit = new SpyLiveKitService(onDelete: _ => operationLog.Add("delete_room"));
        var payment = new SpyPaymentService();

        var sut = CreateService(uow, payment, hub, liveKit);

        // Act
        await sut.AutoCompleteElapsedScheduledConsultationsAsync();

        // Assert — signal and delete must appear before commit (which persists status)
        var signalIdx = operationLog.IndexOf($"signal:{ConsultationRealtimeEvents.ConsultationCallEnded}");
        var deleteIdx = operationLog.IndexOf("delete_room");
        var commitIdx = operationLog.IndexOf("commit");

        Assert.True(signalIdx >= 0, "ConsultationCallEnded signal was not sent");
        Assert.True(deleteIdx >= 0, "DeleteRoomAsync was not called");
        Assert.True(commitIdx >= 0, "CommitAsync was not called");
        Assert.True(signalIdx < commitIdx, "Signal must be sent BEFORE commit");
        Assert.True(deleteIdx < commitIdx, "DeleteRoom must be called BEFORE commit");
    }

    #endregion

    #region 8.2 — Emergency: status Completed + EndTime set

    [Fact]
    public async Task AutoCompleteEmergency_SetsStatusCompletedAndEndTime()
    {
        // Arrange
        var consultationId = Guid.NewGuid();
        var consultation = MakeElapsedEmergencyConsultation(consultationId);
        var uow = new SpyUnitOfWork(emergencyConsultations: new[] { consultation });
        var sut = CreateService(uow);

        // Act
        var count = await sut.AutoCompleteElapsedEmergencyConsultationsAsync();

        // Assert
        Assert.Equal(1, count);
        Assert.Equal(ConsultationStatus.Completed, consultation.Status);
        Assert.NotNull(consultation.EndTime);
    }

    #endregion

    #region 8.3 — SettleConsultationEscrowAsync called for each completed consultation

    [Fact]
    public async Task AutoCompleteScheduled_CallsSettlement_ForEachConsultation()
    {
        // Arrange
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var bookings = new[] { MakeElapsedBooking(id1), MakeElapsedBooking(id2) };
        var uow = new SpyUnitOfWork(bookings: bookings);
        var payment = new SpyPaymentService();
        var sut = CreateService(uow, payment);

        // Act
        await sut.AutoCompleteElapsedScheduledConsultationsAsync();

        // Assert
        Assert.Equal(2, payment.SettledConsultationIds.Count);
        Assert.Contains(id1, payment.SettledConsultationIds);
        Assert.Contains(id2, payment.SettledConsultationIds);
    }

    [Fact]
    public async Task AutoCompleteEmergency_CallsSettlement_ForEachConsultation()
    {
        // Arrange
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var consultations = new[]
        {
            MakeElapsedEmergencyConsultation(id1),
            MakeElapsedEmergencyConsultation(id2)
        };
        var uow = new SpyUnitOfWork(emergencyConsultations: consultations);
        var payment = new SpyPaymentService();
        var sut = CreateService(uow, payment);

        // Act
        await sut.AutoCompleteElapsedEmergencyConsultationsAsync();

        // Assert
        Assert.Equal(2, payment.SettledConsultationIds.Count);
        Assert.Contains(id1, payment.SettledConsultationIds);
        Assert.Contains(id2, payment.SettledConsultationIds);
    }

    #endregion

    #region 8.4 — ConsultationCallEnded signal payload: consultationId + reason=\"timeout\"

    [Fact]
    public async Task AutoCompleteScheduled_SignalPayload_ContainsCorrectFields()
    {
        // Arrange
        var consultationId = Guid.NewGuid();
        var booking = MakeElapsedBooking(consultationId);
        var uow = new SpyUnitOfWork(bookings: new[] { booking });
        var hub = new SpyHubContext();
        var sut = CreateService(uow, hub: hub);

        // Act
        await sut.AutoCompleteElapsedScheduledConsultationsAsync();

        // Assert
        var call = Assert.Single(hub.SendCalls);
        Assert.Equal($"consultation:{consultationId}", call.GroupName);
        Assert.Equal(ConsultationRealtimeEvents.ConsultationCallEnded, call.Method);

        // The payload is an anonymous object; use reflection to verify fields
        var payload = call.Args[0]!;
        var cidProp = payload.GetType().GetProperty("ConsultationId");
        var reasonProp = payload.GetType().GetProperty("Reason");
        Assert.NotNull(cidProp);
        Assert.NotNull(reasonProp);
        Assert.Equal(consultationId, (Guid)cidProp.GetValue(payload)!);
        Assert.Equal(ConsultationRealtimeEvents.ConsultationCallEndReasons.Timeout, (string)reasonProp.GetValue(payload)!);
    }

    #endregion

    #region 8.5 — DeleteRoomAsync called with "consultation-{id}"

    [Fact]
    public async Task AutoCompleteScheduled_DeletesRoom_WithCorrectNameFormat()
    {
        // Arrange
        var consultationId = Guid.NewGuid();
        var booking = MakeElapsedBooking(consultationId);
        var uow = new SpyUnitOfWork(bookings: new[] { booking });
        var liveKit = new SpyLiveKitService();
        var sut = CreateService(uow, liveKit: liveKit);

        // Act
        await sut.AutoCompleteElapsedScheduledConsultationsAsync();

        // Assert
        var deletedRoom = Assert.Single(liveKit.DeletedRoomNames);
        Assert.Equal($"consultation-{consultationId}", deletedRoom);
    }

    [Fact]
    public async Task AutoCompleteEmergency_DeletesRoom_WithCorrectNameFormat()
    {
        // Arrange
        var consultationId = Guid.NewGuid();
        var consultation = MakeElapsedEmergencyConsultation(consultationId);
        var uow = new SpyUnitOfWork(emergencyConsultations: new[] { consultation });
        var liveKit = new SpyLiveKitService();
        var sut = CreateService(uow, liveKit: liveKit);

        // Act
        await sut.AutoCompleteElapsedEmergencyConsultationsAsync();

        // Assert
        var deletedRoom = Assert.Single(liveKit.DeletedRoomNames);
        Assert.Equal($"consultation-{consultationId}", deletedRoom);
    }

    #endregion

    #region 8.6 — SignalR failure → still deletes room and updates status

    [Fact]
    public async Task AutoCompleteScheduled_SignalRFails_StillDeletesRoomAndUpdatesStatus()
    {
        // Arrange
        var consultationId = Guid.NewGuid();
        var booking = MakeElapsedBooking(consultationId);
        var uow = new SpyUnitOfWork(bookings: new[] { booking });
        var hub = new SpyHubContext(throwOnSend: true);
        var liveKit = new SpyLiveKitService();
        var payment = new SpyPaymentService();
        var sut = CreateService(uow, payment, hub, liveKit);

        // Act
        var count = await sut.AutoCompleteElapsedScheduledConsultationsAsync();

        // Assert — room deleted, status updated, settlement called
        Assert.Equal(1, count);
        Assert.Single(liveKit.DeletedRoomNames);
        Assert.Equal(ConsultationStatus.Completed, booking.Consultation!.Status);
        Assert.Single(payment.SettledConsultationIds);
    }

    #endregion

    #region 8.7 — DeleteRoomAsync failure → still updates status to Completed

    [Fact]
    public async Task AutoCompleteScheduled_DeleteRoomFails_StillUpdatesStatusCompleted()
    {
        // Arrange
        var consultationId = Guid.NewGuid();
        var booking = MakeElapsedBooking(consultationId);
        var uow = new SpyUnitOfWork(bookings: new[] { booking });
        var liveKit = new SpyLiveKitService(throwOnDelete: true);
        var payment = new SpyPaymentService();
        var sut = CreateService(uow, payment, liveKit: liveKit);

        // Act
        var count = await sut.AutoCompleteElapsedScheduledConsultationsAsync();

        // Assert
        Assert.Equal(1, count);
        Assert.Equal(ConsultationStatus.Completed, booking.Consultation!.Status);
        Assert.NotNull(booking.Consultation.EndTime);
        Assert.Single(payment.SettledConsultationIds);
    }

    [Fact]
    public async Task AutoCompleteEmergency_DeleteRoomFails_StillUpdatesStatusCompleted()
    {
        // Arrange
        var consultationId = Guid.NewGuid();
        var consultation = MakeElapsedEmergencyConsultation(consultationId);
        var uow = new SpyUnitOfWork(emergencyConsultations: new[] { consultation });
        var liveKit = new SpyLiveKitService(throwOnDelete: true);
        var payment = new SpyPaymentService();
        var sut = CreateService(uow, payment, liveKit: liveKit);

        // Act
        var count = await sut.AutoCompleteElapsedEmergencyConsultationsAsync();

        // Assert
        Assert.Equal(1, count);
        Assert.Equal(ConsultationStatus.Completed, consultation.Status);
        Assert.NotNull(consultation.EndTime);
        Assert.Single(payment.SettledConsultationIds);
    }

    #endregion

    #region 8.8 — Settlement failure → log error, consultation still Completed

    [Fact]
    public async Task AutoCompleteScheduled_SettlementFails_ConsultationStillCompleted()
    {
        // Arrange
        var consultationId = Guid.NewGuid();
        var booking = MakeElapsedBooking(consultationId);
        var uow = new SpyUnitOfWork(bookings: new[] { booking });
        var payment = new SpyPaymentService(throwOnSettle: true);
        var sut = CreateService(uow, payment);

        // Act — settlement throws but is caught by the outer try-catch
        var count = await sut.AutoCompleteElapsedScheduledConsultationsAsync();

        // Assert — status was set before settlement; the outer catch prevents count++
        // but the consultation entity itself was already mutated to Completed
        Assert.Equal(ConsultationStatus.Completed, booking.Consultation!.Status);
    }

    [Fact]
    public async Task AutoCompleteEmergency_SettlementFails_ConsultationStillCompleted()
    {
        // Arrange
        var consultationId = Guid.NewGuid();
        var consultation = MakeElapsedEmergencyConsultation(consultationId);
        var uow = new SpyUnitOfWork(emergencyConsultations: new[] { consultation });
        var payment = new SpyPaymentService(throwOnSettle: true);
        var sut = CreateService(uow, payment);

        // Act
        var count = await sut.AutoCompleteElapsedEmergencyConsultationsAsync();

        // Assert
        Assert.Equal(ConsultationStatus.Completed, consultation.Status);
        Assert.NotNull(consultation.EndTime);
    }

    #endregion

    #region 8.9 — One consultation error → others still processed

    [Fact]
    public async Task AutoCompleteScheduled_OneBookingErrors_OthersStillProcessed()
    {
        // Arrange — first booking will fail on commit, second should succeed
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var bookings = new[] { MakeElapsedBooking(id1), MakeElapsedBooking(id2) };
        var failOnFirst = true;
        var uow = new SpyUnitOfWork(
            bookings: bookings,
            onCommit: () =>
            {
                if (failOnFirst)
                {
                    failOnFirst = false;
                    throw new Exception("Simulated DB error on first booking");
                }
            });
        var payment = new SpyPaymentService();
        var sut = CreateService(uow, payment);

        // Act
        var count = await sut.AutoCompleteElapsedScheduledConsultationsAsync();

        // Assert — second booking was still processed
        Assert.Equal(1, count);
        Assert.Single(payment.SettledConsultationIds);
        Assert.Contains(id2, payment.SettledConsultationIds);
    }

    [Fact]
    public async Task AutoCompleteEmergency_OneConsultationErrors_OthersStillProcessed()
    {
        // Arrange
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var consultations = new[]
        {
            MakeElapsedEmergencyConsultation(id1),
            MakeElapsedEmergencyConsultation(id2)
        };
        var failOnFirst = true;
        var uow = new SpyUnitOfWork(
            emergencyConsultations: consultations,
            onCommit: () =>
            {
                if (failOnFirst)
                {
                    failOnFirst = false;
                    throw new Exception("Simulated DB error on first consultation");
                }
            });
        var payment = new SpyPaymentService();
        var sut = CreateService(uow, payment);

        // Act
        var count = await sut.AutoCompleteElapsedEmergencyConsultationsAsync();

        // Assert
        Assert.Equal(1, count);
        Assert.Single(payment.SettledConsultationIds);
        Assert.Contains(id2, payment.SettledConsultationIds);
    }

    #endregion

    #region Helpers

    private static BookingService CreateService(
        SpyUnitOfWork? uow = null,
        SpyPaymentService? payment = null,
        SpyHubContext? hub = null,
        SpyLiveKitService? liveKit = null)
    {
        return new BookingService(
            uow ?? new SpyUnitOfWork(),
            payment ?? new SpyPaymentService(),
            hub ?? new SpyHubContext(),
            liveKit ?? new SpyLiveKitService(),
            NullLogger<BookingService>.Instance);
    }

    private static ConsultationBooking MakeElapsedBooking(Guid consultationId)
    {
        var slotEnd = DateTime.UtcNow.AddMinutes(-5);
        var consultation = new Consultation
        {
            Id = consultationId,
            CallerId = Guid.NewGuid(),
            CalleeId = Guid.NewGuid(),
            RoomId = $"consultation-{consultationId}",
            StartTime = slotEnd.AddMinutes(-30),
            Status = ConsultationStatus.Ongoing,
            Type = ConsultationType.Scheduled
        };
        return new ConsultationBooking
        {
            Id = Guid.NewGuid(),
            UserId = consultation.CallerId,
            ExpertId = consultation.CalleeId,
            Price = 100m,
            BookedAt = DateTime.UtcNow.AddHours(-1),
            Status = BookingStatus.Confirmed,
            ConsultationId = consultationId,
            Consultation = consultation,
            TimeSlotId = Guid.NewGuid(),
            TimeSlot = new ExpertTimeSlot
            {
                Id = Guid.NewGuid(),
                ExpertId = consultation.CalleeId,
                StartTime = slotEnd.AddMinutes(-30),
                EndTime = slotEnd,
                Status = TimeSlotStatus.Reserved
            }
        };
    }

    private static Consultation MakeElapsedEmergencyConsultation(Guid consultationId)
    {
        return new Consultation
        {
            Id = consultationId,
            CallerId = Guid.NewGuid(),
            CalleeId = Guid.NewGuid(),
            RoomId = $"consultation-{consultationId}",
            StartTime = DateTime.UtcNow.AddMinutes(-35), // 35 min ago → past 30-min threshold
            Status = ConsultationStatus.Ongoing,
            Type = ConsultationType.Emergency
        };
    }

    #endregion

    #region Spy / Stub implementations

    /// <summary>
    /// Spy UnitOfWork that returns pre-configured data from GetListAsync
    /// and records commits.
    /// </summary>
    private sealed class SpyUnitOfWork : IUnitOfWork<SnakeAidDbContext>
    {
        private readonly IReadOnlyList<ConsultationBooking> _bookings;
        private readonly IReadOnlyList<Consultation> _emergencyConsultations;
        private readonly Action? _onCommit;

        public SpyUnitOfWork(
            IEnumerable<ConsultationBooking>? bookings = null,
            IEnumerable<Consultation>? emergencyConsultations = null,
            Action? onCommit = null)
        {
            _bookings = bookings?.ToList() ?? new List<ConsultationBooking>();
            _emergencyConsultations = emergencyConsultations?.ToList() ?? new List<Consultation>();
            _onCommit = onCommit;
        }

        public SnakeAidDbContext Context => throw new NotImplementedException();

        public IGenericRepository<TEntity> GetRepository<TEntity>() where TEntity : class
        {
            if (typeof(TEntity) == typeof(ConsultationBooking))
                return (IGenericRepository<TEntity>)(object)new StubRepo<ConsultationBooking>(_bookings.ToList());
            if (typeof(TEntity) == typeof(Consultation))
                return (IGenericRepository<TEntity>)(object)new StubRepo<Consultation>(_emergencyConsultations.ToList());
            if (typeof(TEntity) == typeof(ExpertTimeSlot))
                return (IGenericRepository<TEntity>)(object)new StubRepo<ExpertTimeSlot>(new List<ExpertTimeSlot>());
            return new StubRepo<TEntity>(new List<TEntity>());
        }

        public Task<int> CommitAsync()
        {
            _onCommit?.Invoke();
            return Task.FromResult(1);
        }

        public Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation) => operation();
        public Task ExecuteInTransactionAsync(Func<Task> operation) => operation();
        public int Commit() => 1;
        public Task RollbackAsync() => Task.CompletedTask;
        public void ClearChangeTracker() { }
        public void Dispose() { }
    }

    private sealed class StubRepo<T> : IGenericRepository<T> where T : class
    {
        private readonly List<T> _data;
        public StubRepo(List<T> data) => _data = data;

        public Task<ICollection<T>> GetListAsync(
            Expression<Func<T, bool>>? predicate = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            Func<IQueryable<T>, IQueryable<T>>? include = null,
            int? take = null,
            bool asNoTracking = true,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<T> result = _data;
            // We skip predicate evaluation — test data is already pre-filtered
            return Task.FromResult<ICollection<T>>(result.ToList());
        }

        public bool Update(T entity) => true;
        public Task<T> InsertAsync(T entity, CancellationToken ct = default) => Task.FromResult(entity);
        public Task<IEnumerable<T>> InsertRangeAsync(IEnumerable<T> entities, CancellationToken ct = default) => Task.FromResult(entities);
        public bool UpdateProperties(T entity, params Expression<Func<T, object>>[] props) => true;
        public bool UpdateRange(IEnumerable<T> entities) => true;
        public bool Delete(T entity) => true;
        public bool DeleteRange(IEnumerable<T> entities) => true;
        public Task<T?> GetByIdAsync<TKey>(TKey id) => Task.FromResult<T?>(default);
        public Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>>? predicate = null, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, Func<IQueryable<T>, IQueryable<T>>? include = null, bool asNoTracking = true, CancellationToken cancellationToken = default) => Task.FromResult<T?>(default);
        public Task<TResult?> FirstOrDefaultAsync<TResult>(Expression<Func<T, TResult>> selector, Expression<Func<T, bool>>? predicate = null, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, Func<IQueryable<T>, IQueryable<T>>? include = null, bool asNoTracking = true, CancellationToken cancellationToken = default) => Task.FromResult<TResult?>(default);
        public Task<ICollection<TResult>> GetListAsync<TResult>(Expression<Func<T, TResult>> selector, Expression<Func<T, bool>>? predicate = null, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, Func<IQueryable<T>, IQueryable<T>>? include = null, int? take = null, bool asNoTracking = true, CancellationToken cancellationToken = default) => Task.FromResult<ICollection<TResult>>(new List<TResult>());
        public Task<SnakeAid.Core.Meta.PagedData<T>> GetPagingListAsync(Expression<Func<T, bool>>? predicate = null, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, Func<IQueryable<T>, IQueryable<T>>? include = null, int page = 1, int size = 10, bool asNoTracking = true, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<SnakeAid.Core.Meta.PagedData<TResult>> GetPagingListAsync<TResult>(Expression<Func<T, TResult>> selector, Expression<Func<T, bool>>? predicate = null, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, Func<IQueryable<T>, IQueryable<T>>? include = null, int page = 1, int size = 10, bool asNoTracking = true, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<HashSet<TProperty>> GetExistingValuesAsync<TProperty>(Expression<Func<T, TProperty>> selector, List<TProperty> candidates, Expression<Func<T, bool>>? additionalFilter = null, CancellationToken cancellationToken = default) => Task.FromResult(new HashSet<TProperty>());
        public IQueryable<T> CreateBaseQuery(bool asNoTracking = true) => _data.AsQueryable();
        public void Dispose() { }
    }

    /// <summary>
    /// Spy IHubContext that records SendAsync calls and optionally throws.
    /// </summary>
    private sealed class SpyHubContext : IHubContext<ConsultationHub>
    {
        private readonly bool _throwOnSend;
        public List<SignalCall> SendCalls { get; } = new();

        public SpyHubContext(
            bool throwOnSend = false,
            Action<string, string, object?[]>? onSend = null)
        {
            _throwOnSend = throwOnSend;
            _onSend = onSend;
        }

        private readonly Action<string, string, object?[]>? _onSend;

        public IHubClients Clients => new SpyHubClients(this);
        public IGroupManager Groups => throw new NotImplementedException();

        internal void RecordSend(string groupName, string method, object?[] args)
        {
            SendCalls.Add(new SignalCall(groupName, method, args));
            _onSend?.Invoke(groupName, method, args);
            if (_throwOnSend)
                throw new InvalidOperationException("Simulated SignalR failure");
        }

        public record SignalCall(string GroupName, string Method, object?[] Args);

        private sealed class SpyHubClients : IHubClients
        {
            private readonly SpyHubContext _owner;
            public SpyHubClients(SpyHubContext owner) => _owner = owner;

            public IClientProxy All => new SpyClientProxy(_owner, "all");
            public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => All;
            public IClientProxy Client(string connectionId) => All;
            public IClientProxy Clients(IReadOnlyList<string> connectionIds) => All;
            public IClientProxy Group(string groupName) => new SpyClientProxy(_owner, groupName);
            public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => Group(groupName);
            public IClientProxy Groups(IReadOnlyList<string> groupNames) => All;
            public IClientProxy User(string userId) => All;
            public IClientProxy Users(IReadOnlyList<string> userIds) => All;
        }

        private sealed class SpyClientProxy : IClientProxy
        {
            private readonly SpyHubContext _owner;
            private readonly string _groupName;
            public SpyClientProxy(SpyHubContext owner, string groupName)
            {
                _owner = owner;
                _groupName = groupName;
            }

            public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
            {
                _owner.RecordSend(_groupName, method, args);
                return Task.CompletedTask;
            }
        }
    }

    /// <summary>
    /// Spy LiveKit service that records DeleteRoomAsync calls.
    /// </summary>
    private sealed class SpyLiveKitService : ILiveKitService
    {
        private readonly bool _throwOnDelete;
        private readonly Action<string>? _onDelete;
        public List<string> DeletedRoomNames { get; } = new();

        public SpyLiveKitService(bool throwOnDelete = false, Action<string>? onDelete = null)
        {
            _throwOnDelete = throwOnDelete;
            _onDelete = onDelete;
        }

        public Task DeleteRoomAsync(string roomName, CancellationToken cancellationToken = default)
        {
            DeletedRoomNames.Add(roomName);
            _onDelete?.Invoke(roomName);
            if (_throwOnDelete)
                throw new InvalidOperationException("Simulated LiveKit failure");
            return Task.CompletedTask;
        }

        public string GenerateAccessToken(string identity, string roomName, VideoGrants grants, string? metadata = null, TimeSpan? ttl = null) => string.Empty;
        public Task<RoomInfoResponse> CreateRoomAsync(string roomName, int maxParticipants = 2, int emptyTimeoutSeconds = 600, CancellationToken cancellationToken = default) => Task.FromResult(new RoomInfoResponse());
        public Task<List<RoomInfoResponse>> ListRoomsAsync(CancellationToken cancellationToken = default) => Task.FromResult(new List<RoomInfoResponse>());
        public LiveKitWebhookPayload? ValidateWebhook(string body, string authorizationHeader) => null;
    }

    /// <summary>
    /// Spy payment service that records SettleConsultationEscrowAsync calls.
    /// </summary>
    private sealed class SpyPaymentService : IConsultationPaymentService
    {
        private readonly bool _throwOnSettle;
        public List<Guid> SettledConsultationIds { get; } = new();

        public SpyPaymentService(bool throwOnSettle = false)
        {
            _throwOnSettle = throwOnSettle;
        }

        public Task<bool> SettleConsultationEscrowAsync(Guid consultationId, CancellationToken cancellationToken = default)
        {
            SettledConsultationIds.Add(consultationId);
            if (_throwOnSettle)
                throw new InvalidOperationException("Simulated settlement failure");
            return Task.FromResult(true);
        }

        public Task<ConsultationPaymentResponse> ConfirmConsultationPaymentAsync(Guid transactionId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PayOsWebhookResponse> ConfirmConsultationPaymentByOrderCodeAsync(long orderCode, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<int> ExpireEmergencyRequestsAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<bool> IsConsultationPayOsOrderCodeAsync(long orderCode, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<ConsultationPaymentResponse> PayEmergencyRequestAsync(Guid userId, Guid requestId, ProcessConsultationPaymentRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<ConsultationPaymentResponse> PayScheduledBookingAsync(Guid userId, Guid bookingId, ProcessConsultationPaymentRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PayOsWebhookResponse> ProcessConsultationWebhookAsync(string rawPayload, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> RefundEmergencyEscrowAsync(Guid requestId, string reason, CancellationToken cancellationToken = default) => Task.FromResult(false);
    }

    #endregion
}
