using System.Reflection;
using System.Security.Claims;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SnakeAid.Api.Controllers;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Messages.Notifications;
using SnakeAid.Core.Meta;
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

namespace SnakeAid.Tests.Integration;

public class ConsultationBookingsControllerIntegrationTests
{
    [Fact]
    public async Task GetExpertBookings_ShouldReturnConfirmedAndCompletedBookings_ForCurrentExpert()
    {
        var expertId = Guid.NewGuid();
        var otherExpertId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var confirmedConsultationId = Guid.NewGuid();
        var completedConsultationId = Guid.NewGuid();

        await using var db = CreateDbContext();
        await SeedAccountAsync(db, expertId, "Expert One", AccountRole.Expert);
        await SeedAccountAsync(db, otherExpertId, "Expert Two", AccountRole.Expert);
        await SeedAccountAsync(db, userId, "Member One", AccountRole.User);
        await SeedAccountAsync(db, otherUserId, "Member Two", AccountRole.User);

        db.ExpertProfiles.AddRange(
            new ExpertProfile { AccountId = expertId, Biography = "Expert one", ConsultationFee = 150_000m },
            new ExpertProfile { AccountId = otherExpertId, Biography = "Expert two", ConsultationFee = 150_000m });

        var confirmedSlotId = Guid.NewGuid();
        var completedSlotId = Guid.NewGuid();
        var pendingSlotId = Guid.NewGuid();
        var foreignSlotId = Guid.NewGuid();

        db.ExpertTimeSlots.AddRange(
            NewSlot(confirmedSlotId, expertId, new DateTime(2026, 3, 10, 8, 0, 0, DateTimeKind.Utc)),
            NewSlot(completedSlotId, expertId, new DateTime(2026, 3, 10, 9, 0, 0, DateTimeKind.Utc), TimeSlotStatus.Booked),
            NewSlot(pendingSlotId, expertId, new DateTime(2026, 3, 10, 10, 0, 0, DateTimeKind.Utc), TimeSlotStatus.Reserved),
            NewSlot(foreignSlotId, otherExpertId, new DateTime(2026, 3, 10, 11, 0, 0, DateTimeKind.Utc)));

        db.Consultations.AddRange(
            new Consultation
            {
                Id = confirmedConsultationId,
                CallerId = userId,
                CalleeId = expertId,
                RoomId = $"consultation-{confirmedConsultationId:N}",
                StartTime = new DateTime(2026, 3, 10, 8, 0, 0, DateTimeKind.Utc),
                Status = ConsultationStatus.Scheduled,
                Type = ConsultationType.Scheduled
            },
            new Consultation
            {
                Id = completedConsultationId,
                CallerId = userId,
                CalleeId = expertId,
                RoomId = $"consultation-{completedConsultationId:N}",
                StartTime = new DateTime(2026, 3, 10, 9, 0, 0, DateTimeKind.Utc),
                EndTime = new DateTime(2026, 3, 10, 9, 30, 0, DateTimeKind.Utc),
                Status = ConsultationStatus.Completed,
                Type = ConsultationType.Scheduled
            });

        db.ConsultationBookings.AddRange(
            new ConsultationBooking
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ExpertId = expertId,
                Price = 150_000m,
                BookedAt = DateTime.UtcNow,
                PaymentDeadline = DateTime.UtcNow.AddMinutes(15),
                Status = BookingStatus.Confirmed,
                TimeSlotId = confirmedSlotId,
                ConsultationId = confirmedConsultationId,
                ProblemDescription = "Confirmed booking"
            },
            new ConsultationBooking
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ExpertId = expertId,
                Price = 150_000m,
                BookedAt = DateTime.UtcNow.AddMinutes(-30),
                PaymentDeadline = DateTime.UtcNow.AddMinutes(-15),
                Status = BookingStatus.Completed,
                TimeSlotId = completedSlotId,
                ConsultationId = completedConsultationId,
                ProblemDescription = "Completed booking"
            },
            new ConsultationBooking
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ExpertId = expertId,
                Price = 150_000m,
                BookedAt = DateTime.UtcNow,
                PaymentDeadline = DateTime.UtcNow.AddMinutes(15),
                Status = BookingStatus.PendingPayment,
                TimeSlotId = pendingSlotId,
                ConsultationId = null,
                ProblemDescription = "Pending payment booking"
            },
            new ConsultationBooking
            {
                Id = Guid.NewGuid(),
                UserId = otherUserId,
                ExpertId = otherExpertId,
                Price = 150_000m,
                BookedAt = DateTime.UtcNow,
                PaymentDeadline = DateTime.UtcNow.AddMinutes(15),
                Status = BookingStatus.Confirmed,
                TimeSlotId = foreignSlotId,
                ConsultationId = null,
                ProblemDescription = "Another expert booking"
            });

        await db.SaveChangesAsync();

        var bookingService = new BookingService(
            new UnitOfWork<SnakeAidDbContext>(db),
            new FakeConsultationPaymentService(),
            new NoOpHubContext(),
            new NoOpLiveKitService(),
            new RecordingNotificationQueueService(),
            NullLogger<BookingService>.Instance);

        var controller = BuildController(bookingService, expertId, "Expert");

        var result = await controller.GetExpertBookings();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<ApiResponse<IEnumerable<ConsultationBookingResponse>>>(ok.Value);
        var items = payload.Data!.ToList();

        Assert.Equal(2, items.Count);
        Assert.All(items, item => Assert.Equal(expertId, item.ExpertId));
        Assert.All(items, item => Assert.Equal("Member One", item.UserName));
        Assert.DoesNotContain(items, item => item.Status == BookingStatus.PendingPayment);
        Assert.Contains(items, item => item.ConsultationId == confirmedConsultationId);
        Assert.Contains(items, item => item.ConsultationId == completedConsultationId);
        Assert.All(items, item => Assert.False(string.IsNullOrWhiteSpace(item.RoomId)));
    }

    private static ConsultationScheduledController BuildController(IBookingService bookingService, Guid userId, string role)
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Role, role)
                },
                "TestAuth"))
        };

        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var mapper = new Mapper(TypeAdapterConfig.GlobalSettings);

        var controller = new ConsultationScheduledController(
            NullLogger<ConsultationScheduledController>.Instance,
            httpContextAccessor,
            mapper,
            bookingService);

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    private static ExpertTimeSlot NewSlot(Guid slotId, Guid expertId, DateTime startTimeUtc, TimeSlotStatus status = TimeSlotStatus.Available)
    {
        return new ExpertTimeSlot
        {
            Id = slotId,
            ExpertId = expertId,
            StartTime = startTimeUtc,
            EndTime = startTimeUtc.AddMinutes(30),
            Status = status
        };
    }

    private static async Task SeedAccountAsync(SnakeAidDbContext db, Guid id, string fullName, AccountRole role)
    {
        db.Set<Account>().Add(new Account
        {
            Id = id,
            FullName = fullName,
            UserName = $"{fullName.Replace(" ", ".").ToLowerInvariant()}",
            NormalizedUserName = fullName.Replace(" ", ".").ToUpperInvariant(),
            Email = $"{id:N}@test.local",
            NormalizedEmail = $"{id:N}@TEST.LOCAL",
            IsActive = true,
            Role = role
        });

        await db.SaveChangesAsync();
    }

    private static SnakeAidDbContext CreateDbContext()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<SnakeAidDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new ConsultationBookingsSqliteDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private sealed class FakeConsultationPaymentService : IConsultationPaymentService
    {
        public Task<ConsultationPaymentResponse> ConfirmConsultationPaymentAsync(Guid transactionId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PayOsWebhookResponse> ConfirmConsultationPaymentByOrderCodeAsync(long orderCode, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> IsConsultationPayOsOrderCodeAsync(long orderCode, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<ConsultationPaymentResponse> PayScheduledBookingAsync(Guid userId, Guid bookingId, ProcessConsultationPaymentRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<ConsultationPaymentResponse> PayEmergencyRequestAsync(Guid userId, Guid requestId, ProcessConsultationPaymentRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PayOsWebhookResponse> ProcessConsultationWebhookAsync(string rawPayload, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> RefundEmergencyEscrowAsync(Guid requestId, string reason, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> RefundScheduledBookingAsync(Guid bookingId, Guid receiverId, string reason, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> CancelPendingScheduledBookingPaymentAsync(Guid bookingId, string reason, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<int> ExpireEmergencyRequestsAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<bool> SettleConsultationEscrowAsync(Guid consultationId, CancellationToken cancellationToken = default) => Task.FromResult(true);
    }

    private sealed class RecordingNotificationQueueService : INotificationQueueService
    {
        public Task PublishAsync(NotificationMessage message, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task PublishBulkAsync(
            IEnumerable<NotificationMessage> messages,
            IEnumerable<AppNotification> appNotifications,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<int> BroadcastAsync(
            SnakeAid.Core.Requests.Notification.AdminBroadcastNotificationRequest request,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
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

    private sealed class ConsultationBookingsSqliteDbContext : SnakeAidDbContext
    {
        public ConsultationBookingsSqliteDbContext(DbContextOptions<SnakeAidDbContext> options)
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
                typeof(ConsultationBooking)
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
        }
    }
}
