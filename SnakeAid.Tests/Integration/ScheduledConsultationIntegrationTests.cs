using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Requests.Consultation;
using SnakeAid.Core.Requests.LiveKit;
using SnakeAid.Core.Responses.Consultation;
using SnakeAid.Core.Responses.LiveKit;
using SnakeAid.Core.Responses.PayOs;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Implements;
using SnakeAid.Service.Hubs;
using SnakeAid.Service.Implements;
using SnakeAid.Service.Interfaces;
using System.Reflection;

namespace SnakeAid.Tests.Integration;

public class ScheduledConsultationIntegrationTests
{
    [Fact]
    public async Task CreateScheduledBookingAsync_ShouldReserveSlot_AndCreateBookingAndConsultation()
    {
        var userId = Guid.NewGuid();
        var expertId = Guid.NewGuid();
        var slotId = Guid.NewGuid();

        await using var db = CreateDbContext();
        await SeedUserAndExpertAsync(db, userId, expertId);
        db.ExpertTimeSlots.Add(new ExpertTimeSlot
        {
            Id = slotId,
            ExpertId = expertId,
            StartTime = new DateTime(2026, 3, 10, 8, 0, 0, DateTimeKind.Utc),
            EndTime = new DateTime(2026, 3, 10, 8, 30, 0, DateTimeKind.Utc),
            Status = TimeSlotStatus.Available
        });
        await db.SaveChangesAsync();

        var bookingService = new BookingService(
            new UnitOfWork<SnakeAidDbContext>(db),
            new FakeConsultationPaymentService(),
            new NoOpHubContext(),
            new NoOpLiveKitService(),
            NullLogger<BookingService>.Instance);
        var response = await bookingService.CreateScheduledBookingAsync(userId, new CreateConsultationBookingRequest
        {
            TimeSlotId = slotId,
            ProblemDescription = "Need urgent guidance for scheduled consultation."
        });

        Assert.Equal(BookingStatus.PendingPayment, response.Status);
        Assert.NotNull(response.ConsultationId);
        Assert.Equal("Need urgent guidance for scheduled consultation.", response.ProblemDescription);

        var slot = await db.ExpertTimeSlots.FirstAsync(s => s.Id == slotId);
        Assert.Equal(TimeSlotStatus.Reserved, slot.Status);

        var booking = await db.ConsultationBookings.FirstAsync(b => b.Id == response.Id);
        Assert.Equal(slotId, booking.TimeSlotId);

        var consultation = await db.Consultations.FirstAsync(c => c.Id == booking.ConsultationId);
        Assert.Equal(ConsultationStatus.Scheduled, consultation.Status);
        Assert.Equal(ConsultationType.Scheduled, consultation.Type);
    }

    [Fact]
    public async Task EndConsultationAsync_ShouldCompleteConsultation_AndBooking_AndMarkSlotBooked()
    {
        var userId = Guid.NewGuid();
        var expertId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var consultationId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();

        await using var db = CreateDbContext();
        await SeedUserAndExpertAsync(db, userId, expertId);

        db.ExpertTimeSlots.Add(new ExpertTimeSlot
        {
            Id = slotId,
            ExpertId = expertId,
            StartTime = new DateTime(2026, 3, 10, 8, 0, 0, DateTimeKind.Utc),
            EndTime = new DateTime(2026, 3, 10, 8, 30, 0, DateTimeKind.Utc),
            Status = TimeSlotStatus.Reserved
        });

        db.Consultations.Add(new Consultation
        {
            Id = consultationId,
            CallerId = userId,
            CalleeId = expertId,
            RoomId = $"consultation-{consultationId}",
            StartTime = new DateTime(2026, 3, 10, 8, 0, 0, DateTimeKind.Utc),
            Status = ConsultationStatus.Ongoing,
            Type = ConsultationType.Scheduled
        });

        db.ConsultationBookings.Add(new ConsultationBooking
        {
            Id = bookingId,
            UserId = userId,
            ExpertId = expertId,
            TimeSlotId = slotId,
            ConsultationId = consultationId,
            Price = 150_000m,
            BookedAt = DateTime.UtcNow,
            PaymentDeadline = DateTime.UtcNow.AddMinutes(15),
            Status = BookingStatus.Confirmed
        });

        await db.SaveChangesAsync();

        var consultationService = new ConsultationService(new UnitOfWork<SnakeAidDbContext>(db), new FakeConsultationPaymentService(), NullLogger<ConsultationService>.Instance);
        await consultationService.EndConsultationAsync(consultationId, userId);

        var consultation = await db.Consultations.FirstAsync(c => c.Id == consultationId);
        var booking = await db.ConsultationBookings.FirstAsync(b => b.Id == bookingId);
        var slot = await db.ExpertTimeSlots.FirstAsync(s => s.Id == slotId);

        Assert.Equal(ConsultationStatus.Completed, consultation.Status);
        Assert.NotNull(consultation.EndTime);
        Assert.Equal(BookingStatus.Completed, booking.Status);
        Assert.Equal(TimeSlotStatus.Booked, slot.Status);
    }

    [Fact]
    public async Task EndConsultationAsync_ShouldSendConsultationCallEnded_AndDeleteRoom_AndCompleteConsultation()
    {
        var userId = Guid.NewGuid();
        var expertId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var consultationId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();

        await using var db = CreateDbContext();
        await SeedUserAndExpertAsync(db, userId, expertId);

        db.ExpertTimeSlots.Add(new ExpertTimeSlot
        {
            Id = slotId,
            ExpertId = expertId,
            StartTime = new DateTime(2026, 3, 10, 8, 0, 0, DateTimeKind.Utc),
            EndTime = new DateTime(2026, 3, 10, 8, 30, 0, DateTimeKind.Utc),
            Status = TimeSlotStatus.Reserved
        });

        db.Consultations.Add(new Consultation
        {
            Id = consultationId,
            CallerId = userId,
            CalleeId = expertId,
            RoomId = $"consultation-{consultationId}",
            StartTime = new DateTime(2026, 3, 10, 8, 0, 0, DateTimeKind.Utc),
            Status = ConsultationStatus.Ongoing,
            Type = ConsultationType.Scheduled
        });

        db.ConsultationBookings.Add(new ConsultationBooking
        {
            Id = bookingId,
            UserId = userId,
            ExpertId = expertId,
            TimeSlotId = slotId,
            ConsultationId = consultationId,
            Price = 150_000m,
            BookedAt = DateTime.UtcNow,
            PaymentDeadline = DateTime.UtcNow.AddMinutes(15),
            Status = BookingStatus.Confirmed
        });

        await db.SaveChangesAsync();

        var hub = new SpyHubContext();
        var liveKit = new SpyLiveKitService();
        var consultationService = new ConsultationService(
            new UnitOfWork<SnakeAidDbContext>(db),
            new FakeConsultationPaymentService(),
            NullLogger<ConsultationService>.Instance,
            hub,
            liveKit);

        await consultationService.EndConsultationAsync(consultationId, userId);

        var hubCall = Assert.Single(hub.SendCalls);
        Assert.Equal($"consultation:{consultationId}", hubCall.GroupName);
        Assert.Equal(ConsultationRealtimeEvents.ConsultationCallEnded, hubCall.Method);
        var payload = hubCall.Args[0]!;
        Assert.Equal(consultationId, (Guid)payload.GetType().GetProperty("ConsultationId")!.GetValue(payload)!);
        Assert.Equal(ConsultationRealtimeEvents.ConsultationCallEndReasons.ParticipantEnded, (string)payload.GetType().GetProperty("Reason")!.GetValue(payload)!);

        var deletedRoom = Assert.Single(liveKit.DeletedRoomNames);
        Assert.Equal($"consultation-{consultationId}", deletedRoom);

        var consultation = await db.Consultations.FirstAsync(c => c.Id == consultationId);
        var booking = await db.ConsultationBookings.FirstAsync(b => b.Id == bookingId);
        var slot = await db.ExpertTimeSlots.FirstAsync(s => s.Id == slotId);

        Assert.Equal(ConsultationStatus.Completed, consultation.Status);
        Assert.NotNull(consultation.EndTime);
        Assert.Equal(BookingStatus.Completed, booking.Status);
        Assert.Equal(TimeSlotStatus.Booked, slot.Status);
    }

    [Fact]
    public async Task CreateConsultationReviewAsync_ShouldCreateFeedback_AndUpdateExpertRating()
    {
        var userId = Guid.NewGuid();
        var expertId = Guid.NewGuid();
        var consultationId = Guid.NewGuid();

        await using var db = CreateDbContext();
        await SeedUserAndExpertAsync(db, userId, expertId);

        db.Consultations.Add(new Consultation
        {
            Id = consultationId,
            CallerId = userId,
            CalleeId = expertId,
            RoomId = $"consultation-{consultationId}",
            StartTime = new DateTime(2026, 3, 10, 8, 0, 0, DateTimeKind.Utc),
            EndTime = new DateTime(2026, 3, 10, 8, 30, 0, DateTimeKind.Utc),
            Status = ConsultationStatus.Completed,
            Type = ConsultationType.Scheduled
        });

        await db.SaveChangesAsync();

        var consultationService = new ConsultationService(new UnitOfWork<SnakeAidDbContext>(db), new FakeConsultationPaymentService(), NullLogger<ConsultationService>.Instance);
        var response = await consultationService.CreateConsultationReviewAsync(consultationId, userId, new CreateConsultationReviewRequest
        {
            Rating = 5,
            Comments = "Very helpful consultation."
        });

        Assert.Equal(FeedbackType.Consultation, response.Type);
        Assert.Equal(5, response.Rating);
        Assert.Equal("Very helpful consultation.", response.Comments);
        Assert.Equal(expertId, response.TargetUserId);

        var expertProfile = await db.ExpertProfiles.FirstAsync(e => e.AccountId == expertId);
        Assert.Equal(1, expertProfile.RatingCount);
        Assert.Equal(5m, expertProfile.Rating);
    }

    private sealed class FakeConsultationPaymentService : IConsultationPaymentService
    {
        public List<Guid> SettledConsultationIds { get; } = new();

        public Task<ConsultationPaymentResponse> ConfirmConsultationPaymentAsync(Guid transactionId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PayOsWebhookResponse> ConfirmConsultationPaymentByOrderCodeAsync(long orderCode, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> IsConsultationPayOsOrderCodeAsync(long orderCode, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<ConsultationPaymentResponse> PayScheduledBookingAsync(Guid userId, Guid bookingId, ProcessConsultationPaymentRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<ConsultationPaymentResponse> PayEmergencyRequestAsync(Guid userId, Guid requestId, ProcessConsultationPaymentRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PayOsWebhookResponse> ProcessConsultationWebhookAsync(string rawPayload, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> RefundEmergencyEscrowAsync(Guid requestId, string reason, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<int> ExpireEmergencyRequestsAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<bool> SettleConsultationEscrowAsync(Guid consultationId, CancellationToken cancellationToken = default)
        {
            SettledConsultationIds.Add(consultationId);
            return Task.FromResult(true);
        }
    }

    private sealed class NoOpHubContext : IHubContext<ConsultationHub>
    {
        public IHubClients Clients { get; } = new NoOpHubClients();
        public IGroupManager Groups => throw new NotImplementedException();

        private sealed class NoOpHubClients : IHubClients
        {
            private readonly IClientProxy _proxy = new NoOpClientProxy();
            public IClientProxy All => _proxy;
            public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => _proxy;
            public IClientProxy Client(string connectionId) => _proxy;
            public IClientProxy Clients(IReadOnlyList<string> connectionIds) => _proxy;
            public IClientProxy Group(string groupName) => _proxy;
            public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => _proxy;
            public IClientProxy Groups(IReadOnlyList<string> groupNames) => _proxy;
            public IClientProxy User(string userId) => _proxy;
            public IClientProxy Users(IReadOnlyList<string> userIds) => _proxy;
        }

        private sealed class NoOpClientProxy : IClientProxy
        {
            public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
                => Task.CompletedTask;
        }
    }

    private sealed class SpyHubContext : IHubContext<ConsultationHub>
    {
        public List<SendCall> SendCalls { get; } = new();
        public IHubClients Clients { get; }
        public IGroupManager Groups => throw new NotImplementedException();

        public SpyHubContext()
        {
            Clients = new SpyHubClients(SendCalls);
        }

        internal sealed record SendCall(string GroupName, string Method, object?[] Args);

        private sealed class SpyHubClients : IHubClients
        {
            private readonly List<SendCall> _sendCalls;

            public SpyHubClients(List<SendCall> sendCalls)
            {
                _sendCalls = sendCalls;
            }

            public IClientProxy All => new SpyClientProxy("all", _sendCalls);
            public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => new SpyClientProxy("all-except", _sendCalls);
            public IClientProxy Client(string connectionId) => new SpyClientProxy(connectionId, _sendCalls);
            public IClientProxy Clients(IReadOnlyList<string> connectionIds) => new SpyClientProxy("clients", _sendCalls);
            public IClientProxy Group(string groupName) => new SpyClientProxy(groupName, _sendCalls);
            public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => new SpyClientProxy(groupName, _sendCalls);
            public IClientProxy Groups(IReadOnlyList<string> groupNames) => new SpyClientProxy("groups", _sendCalls);
            public IClientProxy User(string userId) => new SpyClientProxy(userId, _sendCalls);
            public IClientProxy Users(IReadOnlyList<string> userIds) => new SpyClientProxy("users", _sendCalls);
        }

        private sealed class SpyClientProxy : IClientProxy
        {
            private readonly string _groupName;
            private readonly List<SendCall> _sendCalls;

            public SpyClientProxy(string groupName, List<SendCall> sendCalls)
            {
                _groupName = groupName;
                _sendCalls = sendCalls;
            }

            public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
            {
                _sendCalls.Add(new SendCall(_groupName, method, args));
                return Task.CompletedTask;
            }
        }
    }

    private sealed class NoOpLiveKitService : ILiveKitService
    {
        public string GenerateAccessToken(string identity, string roomName, VideoGrants grants, string? metadata = null, TimeSpan? ttl = null)
            => string.Empty;
        public Task<RoomInfoResponse> CreateRoomAsync(string roomName, int maxParticipants = 2, int emptyTimeoutSeconds = 600, CancellationToken cancellationToken = default)
            => Task.FromResult(new RoomInfoResponse());
        public Task DeleteRoomAsync(string roomName, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
        public Task<List<RoomInfoResponse>> ListRoomsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new List<RoomInfoResponse>());
        public LiveKitWebhookPayload? ValidateWebhook(string body, string authorizationHeader)
            => null;
    }

    private sealed class SpyLiveKitService : ILiveKitService
    {
        public List<string> DeletedRoomNames { get; } = new();

        public string GenerateAccessToken(string identity, string roomName, VideoGrants grants, string? metadata = null, TimeSpan? ttl = null)
            => string.Empty;
        public Task<RoomInfoResponse> CreateRoomAsync(string roomName, int maxParticipants = 2, int emptyTimeoutSeconds = 600, CancellationToken cancellationToken = default)
            => Task.FromResult(new RoomInfoResponse());
        public Task DeleteRoomAsync(string roomName, CancellationToken cancellationToken = default)
        {
            DeletedRoomNames.Add(roomName);
            return Task.CompletedTask;
        }
        public Task<List<RoomInfoResponse>> ListRoomsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new List<RoomInfoResponse>());
        public LiveKitWebhookPayload? ValidateWebhook(string body, string authorizationHeader)
            => null;
    }

    private static SnakeAidDbContext CreateDbContext()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<SnakeAidDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new ScheduledConsultationSqliteDbContext(options);
        context.Database.EnsureCreated();

        return context;
    }

    private static async Task SeedUserAndExpertAsync(SnakeAidDbContext db, Guid userId, Guid expertId)
    {
        db.Set<Account>().Add(new Account
        {
            Id = userId,
            FullName = "Test User",
            UserName = "test.user",
            NormalizedUserName = "TEST.USER",
            Email = "test.user@test.local",
            NormalizedEmail = "TEST.USER@TEST.LOCAL",
            IsActive = true,
            Role = AccountRole.User
        });

        db.Set<Account>().Add(new Account
        {
            Id = expertId,
            FullName = "Test Expert",
            UserName = "test.expert",
            NormalizedUserName = "TEST.EXPERT",
            Email = "test.expert@test.local",
            NormalizedEmail = "TEST.EXPERT@TEST.LOCAL",
            IsActive = true,
            Role = AccountRole.Expert
        });

        db.ExpertProfiles.Add(new ExpertProfile
        {
            AccountId = expertId,
            Biography = "Expert profile for scheduled consultation tests.",
            ConsultationFee = 150_000m,
            IsOnline = true,
            Rating = 0m,
            RatingCount = 0
        });

        await db.SaveChangesAsync();
    }

    private sealed class ScheduledConsultationSqliteDbContext : SnakeAidDbContext
    {
        public ScheduledConsultationSqliteDbContext(DbContextOptions<SnakeAidDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var keep = new HashSet<Type>
            {
                typeof(Account),
                typeof(ExpertProfile),
                typeof(ExpertTimeSlot),
                typeof(Consultation),
                typeof(ConsultationBooking),
                typeof(UserFeedback)
            };

            var dbSetEntityTypes = typeof(SnakeAidDbContext)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
                .Select(p => p.PropertyType.GetGenericArguments()[0])
                .Distinct();

            foreach (var type in dbSetEntityTypes)
            {
                if (!keep.Contains(type))
                {
                    modelBuilder.Ignore(type);
                }
            }

            modelBuilder.Entity<Account>(entity =>
            {
                entity.HasKey(a => a.Id);
            });

            modelBuilder.Entity<ExpertProfile>(entity =>
            {
                entity.HasKey(e => e.AccountId);
                entity.HasOne(e => e.Account)
                    .WithOne()
                    .HasForeignKey<ExpertProfile>(e => e.AccountId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.Ignore(e => e.Specializations);
            });

            modelBuilder.Entity<ExpertTimeSlot>(entity =>
            {
                entity.HasKey(s => s.Id);
                entity.Property(s => s.Version)
                    .IsConcurrencyToken()
                    .ValueGeneratedNever()
                    .HasDefaultValue(0u);
                entity.HasIndex(s => new { s.ExpertId, s.StartTime, s.EndTime }).IsUnique();
                entity.HasOne(s => s.Expert)
                    .WithMany()
                    .HasForeignKey(s => s.ExpertId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Consultation>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.HasOne(c => c.Caller)
                    .WithMany()
                    .HasForeignKey(c => c.CallerId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(c => c.Callee)
                    .WithMany()
                    .HasForeignKey(c => c.CalleeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ConsultationBooking>(entity =>
            {
                entity.HasKey(b => b.Id);
                entity.Property(b => b.Version)
                    .IsConcurrencyToken()
                    .ValueGeneratedNever()
                    .HasDefaultValue(0u);
                entity.HasOne(b => b.User)
                    .WithMany()
                    .HasForeignKey(b => b.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(b => b.Expert)
                    .WithMany()
                    .HasForeignKey(b => b.ExpertId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(b => b.TimeSlot)
                    .WithMany()
                    .HasForeignKey(b => b.TimeSlotId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(b => b.Consultation)
                    .WithMany()
                    .HasForeignKey(b => b.ConsultationId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<UserFeedback>(entity =>
            {
                entity.HasKey(f => f.Id);
                entity.HasOne(f => f.Rater)
                    .WithMany()
                    .HasForeignKey(f => f.RaterId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(f => f.TargetUser)
                    .WithMany()
                    .HasForeignKey(f => f.TargetUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
