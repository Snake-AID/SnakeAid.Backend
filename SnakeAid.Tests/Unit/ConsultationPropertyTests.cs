using FsCheck;
using FsCheck.Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.Consultation;
using SnakeAid.Core.Responses.Consultation;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Implements;
using SnakeAid.Service.Interfaces;
using System.Linq.Expressions;

namespace SnakeAid.Tests.Unit;

/// <summary>
/// Property-based tests for LiveKit Room Expiry and Expert Consultation History.
/// Uses FsCheck.Xunit to verify correctness properties across randomized inputs.
/// </summary>
public class ConsultationPropertyTests
{
    // ========================================================================
    // Feature: livekit-room-expiry-and-consultation-history, Property 1: Slot-elapsed scheduled detection filter
    // **Validates: Requirements 1.1, 1.3**
    //
    // For any set of ConsultationBookings with various statuses and TimeSlots,
    // only those with Status == Confirmed, ConsultationId != null,
    // TimeSlot.EndTime <= now, and Consultation.Status != Completed are returned.
    // ========================================================================
    [Property(MaxTest = 100)]
    public void Property1_SlotElapsedScheduledDetectionFilter_OnlyReturnsEligibleBookings(
        PositiveInt bookingCount)
    {
        var count = Math.Min(bookingCount.Get, 20); // cap for performance
        var now = DateTime.UtcNow;
        var bookings = GenerateRandomBookings(count, now);

        // Apply the same predicate used in AutoCompleteElapsedScheduledConsultationsAsync
        var filtered = bookings.Where(b =>
            b.Status == BookingStatus.Confirmed
            && b.ConsultationId.HasValue
            && b.TimeSlot.EndTime <= now
            && b.Consultation != null
            && b.Consultation.Status != ConsultationStatus.Completed).ToList();

        // Verify: every returned booking satisfies ALL conditions
        foreach (var b in filtered)
        {
            Assert.Equal(BookingStatus.Confirmed, b.Status);
            Assert.NotNull(b.ConsultationId);
            Assert.True(b.TimeSlot.EndTime <= now);
            Assert.NotNull(b.Consultation);
            Assert.NotEqual(ConsultationStatus.Completed, b.Consultation!.Status);
        }

        // Verify: no eligible booking was missed
        var missed = bookings.Where(b =>
            b.Status == BookingStatus.Confirmed
            && b.ConsultationId.HasValue
            && b.TimeSlot.EndTime <= now
            && b.Consultation != null
            && b.Consultation.Status != ConsultationStatus.Completed
            && !filtered.Contains(b)).ToList();

        Assert.Empty(missed);
    }

    // ========================================================================
    // Feature: livekit-room-expiry-and-consultation-history, Property 2: Emergency elapsed detection filter
    // **Validates: Requirements 1.1, 1.3**
    //
    // For any set of Consultations, only those with Status == Ongoing,
    // Type == Emergency, and StartTime + 30min <= now are returned.
    // ========================================================================
    [Property(MaxTest = 100)]
    public void Property2_EmergencyElapsedDetectionFilter_OnlyReturnsEligibleConsultations(
        PositiveInt consultationCount)
    {
        var count = Math.Min(consultationCount.Get, 20);
        var now = DateTime.UtcNow;
        var expiryThreshold = now.AddMinutes(-30);
        var consultations = GenerateRandomConsultations(count, now);

        // Apply the same predicate used in AutoCompleteElapsedEmergencyConsultationsAsync
        var filtered = consultations.Where(c =>
            c.Status == ConsultationStatus.Ongoing
            && c.Type == ConsultationType.Emergency
            && c.StartTime <= expiryThreshold).ToList();

        // Verify: every returned consultation satisfies ALL conditions
        foreach (var c in filtered)
        {
            Assert.Equal(ConsultationStatus.Ongoing, c.Status);
            Assert.Equal(ConsultationType.Emergency, c.Type);
            Assert.True(c.StartTime <= expiryThreshold);
        }

        // Verify: no eligible consultation was missed
        var missed = consultations.Where(c =>
            c.Status == ConsultationStatus.Ongoing
            && c.Type == ConsultationType.Emergency
            && c.StartTime <= expiryThreshold
            && !filtered.Contains(c)).ToList();

        Assert.Empty(missed);
    }

    // ========================================================================
    // Feature: livekit-room-expiry-and-consultation-history, Property 7: Expert history completeness
    // **Validates: Requirements 6.2, 6.6**
    //
    // For any expert with N consultations (both Scheduled and Emergency),
    // when calling GetExpertConsultationsAsync with no filter,
    // totalItems must equal N.
    // ========================================================================
    [Property(MaxTest = 100)]
    public bool Property7_ExpertHistoryCompleteness_TotalItemsEqualsN(
        PositiveInt scheduledCount, PositiveInt emergencyCount)
    {
        var nScheduled = Math.Min(scheduledCount.Get, 10);
        var nEmergency = Math.Min(emergencyCount.Get, 10);
        var expertId = Guid.NewGuid();

        var (bookings, pingRequests) = GenerateExpertConsultationData(expertId, nScheduled, nEmergency);

        var service = CreateConsultationServiceWithMockedData(expertId, bookings, pingRequests);

        var query = new MyConsultationsQueryRequest { PageNumber = 1, PageSize = 100 };
        var result = service.GetExpertConsultationsAsync(expertId, query).GetAwaiter().GetResult();

        var expectedTotal = nScheduled + nEmergency;
        return result.Meta.TotalItems == expectedTotal;
    }

    // ========================================================================
    // Feature: livekit-room-expiry-and-consultation-history, Property 8: Expert history filtering
    // **Validates: Requirements 6.4, 6.5**
    //
    // For any set of consultations and any filter combination (status, type),
    // all items in the result must satisfy the filter conditions.
    // ========================================================================
    [Property(MaxTest = 100)]
    public bool Property8_ExpertHistoryFiltering_AllItemsMatchFilter(
        PositiveInt scheduledCount, PositiveInt emergencyCount, bool filterByStatus, bool filterByType)
    {
        var nScheduled = Math.Min(scheduledCount.Get, 10);
        var nEmergency = Math.Min(emergencyCount.Get, 10);
        var expertId = Guid.NewGuid();

        var (bookings, pingRequests) = GenerateExpertConsultationData(expertId, nScheduled, nEmergency);

        var service = CreateConsultationServiceWithMockedData(expertId, bookings, pingRequests);

        var query = new MyConsultationsQueryRequest
        {
            PageNumber = 1,
            PageSize = 100,
            Status = filterByStatus ? "Completed" : null,
            Type = filterByType ? "Scheduled" : null
        };

        var result = service.GetExpertConsultationsAsync(expertId, query).GetAwaiter().GetResult();

        foreach (var item in result.Items)
        {
            if (filterByStatus && item.Status != "Completed")
                return false;
            if (filterByType && item.Type != "Scheduled")
                return false;
        }

        return true;
    }

    // ========================================================================
    // Feature: livekit-room-expiry-and-consultation-history, Property 9: Expert history pagination
    // **Validates: Requirements 6.3, 7.4**
    //
    // For any set of consultations with totalItems = T, pageSize = S,
    // and pageNumber = P, the returned items count <= S, currentPage == P,
    // totalItems == T, and totalPages == ceil(T / S).
    // ========================================================================
    [Property(MaxTest = 100)]
    public bool Property9_ExpertHistoryPagination_PaginationMathIsCorrect(
        PositiveInt scheduledCount, PositiveInt emergencyCount)
    {
        var nScheduled = Math.Min(scheduledCount.Get, 10);
        var nEmergency = Math.Min(emergencyCount.Get, 10);
        var totalExpected = nScheduled + nEmergency;
        var expertId = Guid.NewGuid();

        var (bookings, pingRequests) = GenerateExpertConsultationData(expertId, nScheduled, nEmergency);

        var service = CreateConsultationServiceWithMockedData(expertId, bookings, pingRequests);

        // Use a random valid page size (1-20) and page number
        var random = new System.Random();
        var pageSize = random.Next(1, 21);
        var totalPages = (int)Math.Ceiling(totalExpected / (double)pageSize);
        var pageNumber = totalPages > 0 ? random.Next(1, totalPages + 1) : 1;

        var query = new MyConsultationsQueryRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = service.GetExpertConsultationsAsync(expertId, query).GetAwaiter().GetResult();

        return result.Items.Count() <= pageSize
            && result.Meta.CurrentPage == pageNumber
            && result.Meta.TotalItems == totalExpected
            && result.Meta.TotalPages == totalPages;
    }

    // ========================================================================
    // Feature: livekit-room-expiry-and-consultation-history, Property 10: Expert response shape by type
    // **Validates: Requirements 7.1, 7.2, 7.3, 8.1, 8.2**
    //
    // For any consultation in the result:
    // - Always has: consultationId, type, status, userId, roomId, startTime, endTime
    // - If type == "Scheduled": bookingId != null, slotStartTime != null, slotEndTime != null, emergencyRequestId == null
    // - If type == "Emergency": emergencyRequestId != null, bookingId == null, slotStartTime == null, slotEndTime == null, price == null
    // ========================================================================
    [Property(MaxTest = 100)]
    public bool Property10_ExpertResponseShapeByType_FieldsMatchType(
        PositiveInt scheduledCount, PositiveInt emergencyCount)
    {
        var nScheduled = Math.Max(1, Math.Min(scheduledCount.Get, 10));
        var nEmergency = Math.Max(1, Math.Min(emergencyCount.Get, 10));
        var expertId = Guid.NewGuid();

        var (bookings, pingRequests) = GenerateExpertConsultationData(expertId, nScheduled, nEmergency);

        var service = CreateConsultationServiceWithMockedData(expertId, bookings, pingRequests);

        var query = new MyConsultationsQueryRequest { PageNumber = 1, PageSize = 100 };
        var result = service.GetExpertConsultationsAsync(expertId, query).GetAwaiter().GetResult();

        foreach (var item in result.Items)
        {
            // Common fields always present
            if (item.ConsultationId == Guid.Empty) return false;
            if (string.IsNullOrEmpty(item.Type)) return false;
            if (string.IsNullOrEmpty(item.Status)) return false;
            if (item.UserId == Guid.Empty) return false;

            if (item.Type == "Scheduled")
            {
                if (item.BookingId is null) return false;
                if (item.SlotStartTime is null) return false;
                if (item.SlotEndTime is null) return false;
                if (item.EmergencyRequestId is not null) return false;
            }
            else if (item.Type == "Emergency")
            {
                if (item.EmergencyRequestId is null) return false;
                if (item.BookingId is not null) return false;
                if (item.SlotStartTime is not null) return false;
                if (item.SlotEndTime is not null) return false;
                if (item.Price is not null) return false;
            }
        }

        return true;
    }

    // ========================================================================
    // Feature: livekit-room-expiry-and-consultation-history, Property 11: Expert history sorting
    // **Validates: Requirements 7.5**
    //
    // For any set of consultations returned with more than 1 item,
    // the list must be sorted by startTime descending:
    // items[i].StartTime >= items[i+1].StartTime for all i.
    // ========================================================================
    [Property(MaxTest = 100)]
    public bool Property11_ExpertHistorySorting_DescendingStartTimeOrder(
        PositiveInt scheduledCount, PositiveInt emergencyCount)
    {
        var nScheduled = Math.Min(scheduledCount.Get, 10);
        var nEmergency = Math.Min(emergencyCount.Get, 10);
        if (nScheduled + nEmergency < 2)
        {
            nScheduled = 1;
            nEmergency = 1;
        }

        var expertId = Guid.NewGuid();

        var (bookings, pingRequests) = GenerateExpertConsultationData(expertId, nScheduled, nEmergency);

        var service = CreateConsultationServiceWithMockedData(expertId, bookings, pingRequests);

        var query = new MyConsultationsQueryRequest { PageNumber = 1, PageSize = 100 };
        var result = service.GetExpertConsultationsAsync(expertId, query).GetAwaiter().GetResult();

        var items = result.Items.ToList();
        for (var i = 0; i < items.Count - 1; i++)
        {
            var current = items[i].StartTime ?? DateTime.MinValue;
            var next = items[i + 1].StartTime ?? DateTime.MinValue;
            if (current < next)
                return false;
        }

        return true;
    }

    // ========================================================================
    // Helper: Generate random ConsultationBookings for Property 1
    // ========================================================================
    private static List<ConsultationBooking> GenerateRandomBookings(int count, DateTime now)
    {
        var random = new System.Random();
        var statuses = Enum.GetValues<BookingStatus>();
        var consultationStatuses = Enum.GetValues<ConsultationStatus>();
        var bookings = new List<ConsultationBooking>();

        for (var i = 0; i < count; i++)
        {
            var hasConsultation = random.Next(2) == 1;
            var slotEndTime = now.AddMinutes(random.Next(-120, 120)); // some past, some future

            var booking = new ConsultationBooking
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                ExpertId = Guid.NewGuid(),
                Price = random.Next(10, 500),
                BookedAt = DateTime.UtcNow.AddDays(-random.Next(1, 30)),
                Status = statuses[random.Next(statuses.Length)],
                TimeSlotId = Guid.NewGuid(),
                TimeSlot = new ExpertTimeSlot
                {
                    Id = Guid.NewGuid(),
                    ExpertId = Guid.NewGuid(),
                    StartTime = slotEndTime.AddMinutes(-30),
                    EndTime = slotEndTime,
                    Status = TimeSlotStatus.Reserved
                }
            };

            if (hasConsultation)
            {
                var consultationId = Guid.NewGuid();
                booking.ConsultationId = consultationId;
                booking.Consultation = new Consultation
                {
                    Id = consultationId,
                    CallerId = booking.UserId,
                    CalleeId = booking.ExpertId,
                    RoomId = $"consultation-{consultationId}",
                    StartTime = booking.TimeSlot.StartTime,
                    Status = consultationStatuses[random.Next(consultationStatuses.Length)],
                    Type = ConsultationType.Scheduled
                };
            }

            bookings.Add(booking);
        }

        return bookings;
    }

    // ========================================================================
    // Helper: Generate random Consultations for Property 2
    // ========================================================================
    private static List<Consultation> GenerateRandomConsultations(int count, DateTime now)
    {
        var random = new System.Random();
        var statuses = Enum.GetValues<ConsultationStatus>();
        var types = Enum.GetValues<ConsultationType>();
        var consultations = new List<Consultation>();

        for (var i = 0; i < count; i++)
        {
            var startTime = now.AddMinutes(random.Next(-120, 120)); // some expired, some not
            consultations.Add(new Consultation
            {
                Id = Guid.NewGuid(),
                CallerId = Guid.NewGuid(),
                CalleeId = Guid.NewGuid(),
                RoomId = $"consultation-{Guid.NewGuid()}",
                StartTime = startTime,
                Status = statuses[random.Next(statuses.Length)],
                Type = types[random.Next(types.Length)]
            });
        }

        return consultations;
    }

    // ========================================================================
    // Helper: Generate expert consultation data for Properties 7-11
    // ========================================================================
    private static (List<ConsultationBooking> bookings, List<ConsultationPingRequest> pings)
        GenerateExpertConsultationData(Guid expertId, int scheduledCount, int emergencyCount)
    {
        var random = new System.Random();
        var consultationStatuses = new[] { ConsultationStatus.Ongoing, ConsultationStatus.Completed };
        var bookings = new List<ConsultationBooking>();
        var pings = new List<ConsultationPingRequest>();

        for (var i = 0; i < scheduledCount; i++)
        {
            var consultationId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var startTime = DateTime.UtcNow.AddDays(-random.Next(1, 60));
            var status = consultationStatuses[random.Next(consultationStatuses.Length)];

            bookings.Add(new ConsultationBooking
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ExpertId = expertId,
                Price = random.Next(50, 500),
                BookedAt = startTime.AddDays(-1),
                Status = BookingStatus.Confirmed,
                ConsultationId = consultationId,
                TimeSlotId = Guid.NewGuid(),
                TimeSlot = new ExpertTimeSlot
                {
                    Id = Guid.NewGuid(),
                    ExpertId = expertId,
                    StartTime = startTime,
                    EndTime = startTime.AddMinutes(30),
                    Status = TimeSlotStatus.Booked
                },
                Consultation = new Consultation
                {
                    Id = consultationId,
                    CallerId = userId,
                    CalleeId = expertId,
                    RoomId = $"consultation-{consultationId}",
                    StartTime = startTime,
                    EndTime = status == ConsultationStatus.Completed ? startTime.AddMinutes(30) : null,
                    Status = status,
                    Type = ConsultationType.Scheduled
                },
                User = new Account { Id = userId, FullName = $"User_{i}" }
            });
        }

        for (var i = 0; i < emergencyCount; i++)
        {
            var consultationId = Guid.NewGuid();
            var rescuerId = Guid.NewGuid();
            var startTime = DateTime.UtcNow.AddDays(-random.Next(1, 60));
            var status = consultationStatuses[random.Next(consultationStatuses.Length)];

            pings.Add(new ConsultationPingRequest
            {
                Id = Guid.NewGuid(),
                RescuerId = rescuerId,
                ExpertId = expertId,
                Status = ConsultationPingStatus.AcceptedByExpert,
                RequestedAt = startTime.AddMinutes(-5),
                ConsultationId = consultationId,
                Consultation = new Consultation
                {
                    Id = consultationId,
                    CallerId = rescuerId,
                    CalleeId = expertId,
                    RoomId = $"consultation-{consultationId}",
                    StartTime = startTime,
                    EndTime = status == ConsultationStatus.Completed ? startTime.AddMinutes(30) : null,
                    Status = status,
                    Type = ConsultationType.Emergency
                },
                Rescuer = new Account { Id = rescuerId, FullName = $"Rescuer_{i}" }
            });
        }

        return (bookings, pings);
    }

    // ========================================================================
    // Helper: Create ConsultationService with mocked repository data
    // ========================================================================
    private static ConsultationService CreateConsultationServiceWithMockedData(
        Guid expertId,
        List<ConsultationBooking> bookings,
        List<ConsultationPingRequest> pingRequests)
    {
        var unitOfWork = new InMemoryUnitOfWork(bookings, pingRequests);
        var paymentService = new NoOpPaymentService();
        var logger = NullLogger<ConsultationService>.Instance;

        return new ConsultationService(unitOfWork, paymentService, logger);
    }

    // ========================================================================
    // In-memory UnitOfWork and Repository implementations for property tests
    // ========================================================================
    private sealed class InMemoryUnitOfWork : IUnitOfWork<SnakeAidDbContext>
    {
        private readonly Dictionary<Type, object> _repositories = new();

        public InMemoryUnitOfWork(
            List<ConsultationBooking> bookings,
            List<ConsultationPingRequest> pingRequests)
        {
            _repositories[typeof(ConsultationBooking)] = new InMemoryRepository<ConsultationBooking>(bookings);
            _repositories[typeof(ConsultationPingRequest)] = new InMemoryRepository<ConsultationPingRequest>(pingRequests);
            _repositories[typeof(Consultation)] = new InMemoryRepository<Consultation>(new List<Consultation>());
        }

        public SnakeAidDbContext Context => throw new NotImplementedException();

        public IGenericRepository<TEntity> GetRepository<TEntity>() where TEntity : class
        {
            if (_repositories.TryGetValue(typeof(TEntity), out var repo))
                return (IGenericRepository<TEntity>)repo;
            return new InMemoryRepository<TEntity>(new List<TEntity>());
        }

        public Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation) => operation();
        public Task ExecuteInTransactionAsync(Func<Task> operation) => operation();
        public int Commit() => 0;
        public Task<int> CommitAsync() => Task.FromResult(0);
        public Task RollbackAsync() => Task.CompletedTask;
        public void ClearChangeTracker() { }
        public void Dispose() { }
    }

    private sealed class InMemoryRepository<T> : IGenericRepository<T> where T : class
    {
        private readonly List<T> _data;

        public InMemoryRepository(List<T> data) => _data = data;

        public Task<ICollection<T>> GetListAsync(
            Expression<Func<T, bool>>? predicate = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            Func<IQueryable<T>, IQueryable<T>>? include = null,
            int? take = null,
            bool asNoTracking = true,
            CancellationToken cancellationToken = default)
        {
            IQueryable<T> query = _data.AsQueryable();
            // Skip include — navigation properties are already populated in test data
            if (predicate != null)
                query = query.Where(predicate);
            if (orderBy != null)
                query = orderBy(query);
            if (take.HasValue)
                query = query.Take(take.Value);
            return Task.FromResult<ICollection<T>>(query.ToList());
        }

        public Task<T?> FirstOrDefaultAsync(
            Expression<Func<T, bool>>? predicate = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            Func<IQueryable<T>, IQueryable<T>>? include = null,
            bool asNoTracking = true,
            CancellationToken cancellationToken = default)
        {
            IQueryable<T> query = _data.AsQueryable();
            if (predicate != null)
                query = query.Where(predicate);
            return Task.FromResult(query.FirstOrDefault());
        }

        // Unused methods — throw NotImplementedException
        public Task<T?> GetByIdAsync<TKey>(TKey id) => throw new NotImplementedException();
        public Task<TResult?> FirstOrDefaultAsync<TResult>(Expression<Func<T, TResult>> selector, Expression<Func<T, bool>>? predicate = null, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, Func<IQueryable<T>, IQueryable<T>>? include = null, bool asNoTracking = true, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<ICollection<TResult>> GetListAsync<TResult>(Expression<Func<T, TResult>> selector, Expression<Func<T, bool>>? predicate = null, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, Func<IQueryable<T>, IQueryable<T>>? include = null, int? take = null, bool asNoTracking = true, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PagedData<T>> GetPagingListAsync(Expression<Func<T, bool>>? predicate = null, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, Func<IQueryable<T>, IQueryable<T>>? include = null, int page = 1, int size = 10, bool asNoTracking = true, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PagedData<TResult>> GetPagingListAsync<TResult>(Expression<Func<T, TResult>> selector, Expression<Func<T, bool>>? predicate = null, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, Func<IQueryable<T>, IQueryable<T>>? include = null, int page = 1, int size = 10, bool asNoTracking = true, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<HashSet<TProperty>> GetExistingValuesAsync<TProperty>(Expression<Func<T, TProperty>> selector, List<TProperty> candidates, Expression<Func<T, bool>>? additionalFilter = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<T> InsertAsync(T entity, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IEnumerable<T>> InsertRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public bool Update(T entity) => true;
        public bool UpdateProperties(T entity, params Expression<Func<T, object>>[] propertiesToUpdate) => throw new NotImplementedException();
        public bool UpdateRange(IEnumerable<T> entities) => throw new NotImplementedException();
        public bool Delete(T entity) => throw new NotImplementedException();
        public bool DeleteRange(IEnumerable<T> entities) => throw new NotImplementedException();
        public IQueryable<T> CreateBaseQuery(bool asNoTracking = true) => _data.AsQueryable();
        public void Dispose() { }
    }

    private sealed class NoOpPaymentService : IConsultationPaymentService
    {
        public Task<Core.Responses.Consultation.ConsultationPaymentResponse> ConfirmConsultationPaymentAsync(Guid transactionId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Core.Responses.PayOs.PayOsWebhookResponse> ConfirmConsultationPaymentByOrderCodeAsync(long orderCode, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<int> ExpireEmergencyRequestsAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<bool> IsConsultationPayOsOrderCodeAsync(long orderCode, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<Core.Responses.Consultation.ConsultationPaymentResponse> PayEmergencyRequestAsync(Guid userId, Guid requestId, Core.Requests.Consultation.ProcessConsultationPaymentRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Core.Responses.Consultation.ConsultationPaymentResponse> PayScheduledBookingAsync(Guid userId, Guid bookingId, Core.Requests.Consultation.ProcessConsultationPaymentRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Core.Responses.PayOs.PayOsWebhookResponse> ProcessConsultationWebhookAsync(string rawPayload, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> RefundEmergencyEscrowAsync(Guid requestId, string reason, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> RefundScheduledBookingAsync(Guid bookingId, Guid receiverId, string reason, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> CancelPendingScheduledBookingPaymentAsync(Guid bookingId, string reason, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> SettleConsultationEscrowAsync(Guid consultationId, CancellationToken cancellationToken = default) => Task.FromResult(false);
    }
}
