using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Requests.Consultation;
using SnakeAid.Core.Responses.Consultation;
using SnakeAid.Core.Responses.Consultation.History;
using SnakeAid.Core.Responses.PayOs;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Implements;
using SnakeAid.Service.Implements;
using SnakeAid.Service.Interfaces;
using System.Reflection;

namespace SnakeAid.Tests.Integration;

public class ExpertConsultationPriceResponseTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SnakeAidDbContext _db;
    private readonly ConsultationService _service;

    private readonly Guid _expertId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    private readonly Guid _scheduledConsultationId = Guid.NewGuid();
    private readonly Guid _scheduledBookingId = Guid.NewGuid();
    private readonly Guid _scheduledSlotId = Guid.NewGuid();

    private readonly Guid _emergencyConsultation1Id = Guid.NewGuid();
    private readonly Guid _emergencyConsultation2Id = Guid.NewGuid();
    private readonly Guid _pingRequest1Id = Guid.NewGuid();
    private readonly Guid _pingRequest2Id = Guid.NewGuid();

    private static readonly DateTime ScheduledStart = new(2025, 2, 1, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime EmergencyStart1 = new(2025, 2, 2, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime EmergencyStart2 = new(2025, 2, 3, 11, 0, 0, DateTimeKind.Utc);

    private const decimal ScheduledGrossPrice = 500_000m;
    private const decimal EmergencyGrossPrice = 5_000_000m;
    private const decimal EmergencyNetPrice = 4_000_000m;
    private const string ExpertAvatarUrl = "https://cdn.test.local/avatars/expert-one.png";
    private const string UserAvatarUrl = "https://cdn.test.local/avatars/user-one.png";

    public ExpertConsultationPriceResponseTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<SnakeAidDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new ExpertConsultationPriceSqliteDbContext(options);
        _db.Database.EnsureCreated();

        SeedData();

        var unitOfWork = new UnitOfWork<SnakeAidDbContext>(_db);
        _service = new ConsultationService(
            unitOfWork,
            new FakeConsultationPaymentService(),
            NullLogger<ConsultationService>.Instance);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task GetExpertConsultationsAsync_ScheduledItems_ReturnGrossPrice_AndNullNetPriceUntilPayout()
    {
        var query = new MyConsultationsQueryRequest { PageNumber = 1, PageSize = 10, Type = "Scheduled" };

        var result = await _service.GetExpertConsultationsAsync(_expertId, query);

        var item = Assert.Single(result.Items.OfType<ExpertConsultationHistoryResponse>());
        Assert.Equal("Scheduled", item.Type);
        Assert.Equal(_scheduledConsultationId, item.ConsultationId);
        Assert.Equal(ScheduledGrossPrice, item.GrossPrice);
        Assert.Null(item.NetPrice);
        Assert.Equal(_scheduledBookingId, item.BookingId);
        Assert.Equal(UserAvatarUrl, item.UserAvatarUrl);
    }

    [Fact]
    public async Task GetExpertConsultationsAsync_EmergencyItems_ReturnGrossAndNetFromTransactions()
    {
        var query = new MyConsultationsQueryRequest { PageNumber = 1, PageSize = 10, Type = "Emergency" };

        var result = await _service.GetExpertConsultationsAsync(_expertId, query);

        var items = result.Items
            .OfType<ExpertConsultationHistoryResponse>()
            .ToDictionary(item => item.EmergencyRequestId!.Value);

        Assert.Equal(2, items.Count);
        Assert.Equal(EmergencyGrossPrice, items[_pingRequest1Id].GrossPrice);
        Assert.Equal(EmergencyNetPrice, items[_pingRequest1Id].NetPrice);
        Assert.Equal(UserAvatarUrl, items[_pingRequest1Id].UserAvatarUrl);
        Assert.Null(items[_pingRequest2Id].GrossPrice);
        Assert.Null(items[_pingRequest2Id].NetPrice);
        Assert.Equal(UserAvatarUrl, items[_pingRequest2Id].UserAvatarUrl);
    }

    [Fact]
    public void ExpertConsultationResponse_ShouldExposeUserAvatarUrl_AndNotExpertAvatarUrl()
    {
        var properties = typeof(ExpertConsultationResponse)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name)
            .ToHashSet();

        Assert.Contains(nameof(ExpertConsultationResponse.UserAvatarUrl), properties);
        Assert.DoesNotContain("ExpertAvatarUrl", properties);
    }

    private void SeedData()
    {
        _db.Set<Account>().AddRange(
            new Account { Id = _expertId, FullName = "Expert One", AvatarUrl = ExpertAvatarUrl },
            new Account { Id = _userId, FullName = "User One", AvatarUrl = UserAvatarUrl });

        _db.Set<ExpertTimeSlot>().Add(new ExpertTimeSlot
        {
            Id = _scheduledSlotId,
            ExpertId = _expertId,
            StartTime = ScheduledStart,
            EndTime = ScheduledStart.AddMinutes(30),
            Status = TimeSlotStatus.Booked,
            Version = 0u
        });

        _db.Consultations.AddRange(
            new Consultation
            {
                Id = _scheduledConsultationId,
                CallerId = _userId,
                CalleeId = _expertId,
                RoomId = "room-scheduled",
                StartTime = ScheduledStart,
                EndTime = ScheduledStart.AddMinutes(30),
                Status = ConsultationStatus.Completed,
                Type = ConsultationType.Scheduled
            },
            new Consultation
            {
                Id = _emergencyConsultation1Id,
                CallerId = _userId,
                CalleeId = _expertId,
                RoomId = "room-emergency-1",
                StartTime = EmergencyStart1,
                EndTime = EmergencyStart1.AddMinutes(20),
                Status = ConsultationStatus.Completed,
                Type = ConsultationType.Emergency
            },
            new Consultation
            {
                Id = _emergencyConsultation2Id,
                CallerId = _userId,
                CalleeId = _expertId,
                RoomId = "room-emergency-2",
                StartTime = EmergencyStart2,
                EndTime = EmergencyStart2.AddMinutes(20),
                Status = ConsultationStatus.Completed,
                Type = ConsultationType.Emergency
            });

        _db.Set<ConsultationBooking>().Add(new ConsultationBooking
        {
            Id = _scheduledBookingId,
            UserId = _userId,
            ExpertId = _expertId,
            Price = ScheduledGrossPrice,
            BookedAt = ScheduledStart.AddDays(-1),
            Status = BookingStatus.Completed,
            Version = 0u,
            ConsultationId = _scheduledConsultationId,
            TimeSlotId = _scheduledSlotId,
            PaymentDeadline = ScheduledStart
        });

        _db.ConsultationPingRequests.AddRange(
            new ConsultationPingRequest
            {
                Id = _pingRequest1Id,
                RescuerId = _userId,
                ExpertId = _expertId,
                Status = ConsultationPingStatus.AcceptedByExpert,
                RequestedAt = EmergencyStart1,
                ConsultationId = _emergencyConsultation1Id
            },
            new ConsultationPingRequest
            {
                Id = _pingRequest2Id,
                RescuerId = _userId,
                ExpertId = _expertId,
                Status = ConsultationPingStatus.AcceptedByExpert,
                RequestedAt = EmergencyStart2,
                ConsultationId = _emergencyConsultation2Id
            });

        _db.Set<Transaction>().AddRange(
            new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = _userId,
                ReferenceId = _pingRequest1Id,
                Amount = EmergencyGrossPrice,
                Currency = "VND",
                TransactionType = TransactionType.ConsultationPayment,
                Description = "Emergency consultation payment",
                PaymentMethod = "Internal",
                ExternalTransactionId = $"TEST-PAY-{_pingRequest1Id:N}",
                CreatedAt = EmergencyStart1.AddMinutes(-5)
            },
            new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = _expertId,
                ReferenceId = _emergencyConsultation1Id,
                Amount = EmergencyNetPrice,
                Currency = "VND",
                TransactionType = TransactionType.ExpertPayout,
                Description = "Emergency expert payout",
                PaymentMethod = "Internal",
                ExternalTransactionId = $"TEST-PAYOUT-{_emergencyConsultation1Id:N}",
                CreatedAt = EmergencyStart1.AddMinutes(30)
            });

        _db.SaveChanges();
    }

    private sealed class ExpertConsultationPriceSqliteDbContext : SnakeAidDbContext
    {
        public ExpertConsultationPriceSqliteDbContext(DbContextOptions<SnakeAidDbContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var keep = new[]
            {
                typeof(Account),
                typeof(Consultation),
                typeof(ConsultationBooking),
                typeof(ConsultationPingRequest),
                typeof(ExpertTimeSlot),
                typeof(Transaction)
            };

            var dbSetEntityTypes = typeof(SnakeAidDbContext)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.PropertyType.IsGenericType
                         && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
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

            modelBuilder.Entity<Consultation>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.RoomId).IsRequired(false);
                entity.HasOne(c => c.Caller)
                    .WithMany()
                    .HasForeignKey(c => c.CallerId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(c => c.Callee)
                    .WithMany()
                    .HasForeignKey(c => c.CalleeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ExpertTimeSlot>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Version)
                    .IsConcurrencyToken()
                    .ValueGeneratedNever()
                    .HasDefaultValue(0u);
                entity.HasOne(t => t.Expert)
                    .WithMany()
                    .HasForeignKey(t => t.ExpertId)
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

            modelBuilder.Entity<ConsultationPingRequest>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Status)
                    .HasConversion<int>()
                    .IsRequired();
                entity.HasOne(p => p.Rescuer)
                    .WithMany()
                    .HasForeignKey(p => p.RescuerId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(p => p.Expert)
                    .WithMany()
                    .HasForeignKey(p => p.ExpertId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(p => p.Consultation)
                    .WithMany()
                    .HasForeignKey(p => p.ConsultationId)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.Ignore(p => p.RescueMission);
            });

            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.HasOne(t => t.User)
                    .WithMany()
                    .HasForeignKey(t => t.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }

    private sealed class FakeConsultationPaymentService : IConsultationPaymentService
    {
        public Task<ConsultationPaymentResponse> ConfirmConsultationPaymentAsync(Guid transactionId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PayOsWebhookResponse> ConfirmConsultationPaymentByOrderCodeAsync(long orderCode, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<ConsultationPaymentResponse> PayScheduledBookingAsync(Guid userId, Guid bookingId, ProcessConsultationPaymentRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<ConsultationPaymentResponse> PayEmergencyRequestAsync(Guid userId, Guid requestId, ProcessConsultationPaymentRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> IsConsultationPayOsOrderCodeAsync(long orderCode, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<PayOsWebhookResponse> ProcessConsultationWebhookAsync(string rawPayload, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> RefundEmergencyEscrowAsync(Guid requestId, string reason, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<bool> RefundScheduledBookingAsync(Guid bookingId, Guid receiverId, string reason, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<bool> CancelPendingScheduledBookingPaymentAsync(Guid bookingId, string reason, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<int> ExpireEmergencyRequestsAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<bool> SettleConsultationEscrowAsync(Guid consultationId, CancellationToken cancellationToken = default) => Task.FromResult(true);
    }
}
