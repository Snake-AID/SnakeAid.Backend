using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Requests.Consultation;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Implements;
using SnakeAid.Service.Implements;
using SnakeAid.Service.Interfaces;
using System.Reflection;

namespace SnakeAid.Tests.Integration;

public class ConsultationExpertAbsentIntegrationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SnakeAidDbContext _db;
    private readonly ConsultationService _service;

    private readonly Guid _memberId = Guid.NewGuid();
    private readonly Guid _otherMemberId = Guid.NewGuid();
    private readonly Guid _expertId = Guid.NewGuid();

    private readonly Guid _slotId = Guid.NewGuid();
    private readonly Guid _bookingId = Guid.NewGuid();
    private readonly Guid _scheduledConsultationId = Guid.NewGuid();
    private readonly Guid _completedConsultationId = Guid.NewGuid();
    private readonly Guid _futureConsultationId = Guid.NewGuid();
    private readonly Guid _emergencyConsultationId = Guid.NewGuid();
    private readonly Guid _pingRequestId = Guid.NewGuid();

    public ConsultationExpertAbsentIntegrationTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<SnakeAidDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new ConsultationExpertAbsentSqliteDbContext(options);
        _db.Database.EnsureCreated();

        SeedData();

        _service = new ConsultationService(
            new UnitOfWork<SnakeAidDbContext>(_db),
            new FakeConsultationPaymentService(),
            NullLogger<ConsultationService>.Instance);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task ReportExpertAbsentAsync_ShouldPersistReportAndReturnUpdatedConsultation()
    {
        var result = await _service.ReportExpertAbsentAsync(
            _scheduledConsultationId,
            _memberId,
            new ReportExpertAbsentRequest { CustomerReport = "Expert did not join the room." });

        Assert.Equal(_scheduledConsultationId, result.ConsultationId);
        Assert.Equal("ExpertAbsent", result.Status);
        Assert.Equal("Expert did not join the room.", result.CustomerReport);
        Assert.NotNull(result.CustomerReportSubmittedAt);
        Assert.Equal(_bookingId, result.BookingId);

        var persisted = await _db.Set<Consultation>().SingleAsync(c => c.Id == _scheduledConsultationId);
        Assert.Equal(ConsultationStatus.ExpertAbsent, persisted.Status);
        Assert.Equal("Expert did not join the room.", persisted.CustomerReport);
        Assert.NotNull(persisted.CustomerReportSubmittedAt);
    }

    [Fact]
    public async Task ReportExpertAbsentAsync_BeforeStartTime_ShouldThrowBusinessException()
    {
        await Assert.ThrowsAsync<BusinessException>(() => _service.ReportExpertAbsentAsync(
            _futureConsultationId,
            _memberId,
            new ReportExpertAbsentRequest { CustomerReport = "Expert is absent." }));
    }

    [Fact]
    public async Task ReportExpertAbsentAsync_NonOwner_ShouldThrowForbiddenException()
    {
        await Assert.ThrowsAsync<ForbiddenException>(() => _service.ReportExpertAbsentAsync(
            _scheduledConsultationId,
            _otherMemberId,
            new ReportExpertAbsentRequest { CustomerReport = "Expert is absent." }));
    }

    [Fact]
    public async Task ReportExpertAbsentAsync_Duplicate_ShouldThrowConflictException()
    {
        await Assert.ThrowsAsync<ConflictException>(() => _service.ReportExpertAbsentAsync(
            _completedConsultationId,
            _memberId,
            new ReportExpertAbsentRequest { CustomerReport = "Another report." }));
    }

    [Fact]
    public async Task ReportExpertAbsentAsync_EmergencyConsultation_ShouldThrowBusinessException()
    {
        await Assert.ThrowsAsync<BusinessException>(() => _service.ReportExpertAbsentAsync(
            _emergencyConsultationId,
            _memberId,
            new ReportExpertAbsentRequest { CustomerReport = "Expert is absent." }));
    }

    [Fact]
    public async Task GetMyConsultationsAsync_ShouldIncludeCustomerReportForScheduledConsultations()
    {
        var result = await _service.GetMyConsultationsAsync(_memberId, new MyConsultationsQueryRequest
        {
            PageNumber = 1,
            PageSize = 10,
            Type = "Scheduled"
        });

        var item = Assert.Single(result.Items, i => i.ConsultationId == _completedConsultationId);
        Assert.Equal("Existing customer report", item.CustomerReport);
        Assert.NotNull(item.CustomerReportSubmittedAt);
    }

    [Fact]
    public async Task GetAllConsultationsForAdminAsync_ShouldIncludeCustomerReport()
    {
        var result = await _service.GetAllConsultationsForAdminAsync(new AdminConsultationsQueryRequest
        {
            PageNumber = 1,
            PageSize = 10,
            Type = "Scheduled"
        });

        var item = Assert.Single(result.Items, i => i.ConsultationId == _completedConsultationId);
        Assert.Equal("Existing customer report", item.CustomerReport);
    }

    [Fact]
    public async Task GetConsultationByIdForAdminAsync_ShouldIncludeCustomerReport()
    {
        var result = await _service.GetConsultationByIdForAdminAsync(_completedConsultationId);

        Assert.Equal(_completedConsultationId, result.ConsultationId);
        Assert.Equal("Existing customer report", result.CustomerReport);
        Assert.NotNull(result.CustomerReportSubmittedAt);
    }

    private void SeedData()
    {
        _db.Set<Account>().AddRange(
            CreateAccount(_memberId, "Member One", AccountRole.User),
            CreateAccount(_otherMemberId, "Member Two", AccountRole.User),
            CreateAccount(_expertId, "Expert One", AccountRole.Expert));

        _db.Set<ExpertTimeSlot>().Add(new ExpertTimeSlot
        {
            Id = _slotId,
            ExpertId = _expertId,
            StartTime = DateTime.UtcNow.AddHours(-2),
            EndTime = DateTime.UtcNow.AddHours(-1),
            Status = TimeSlotStatus.Booked
        });

        _db.Set<Consultation>().AddRange(
            new Consultation
            {
                Id = _scheduledConsultationId,
                CallerId = _memberId,
                CalleeId = _expertId,
                RoomId = "room-scheduled",
                StartTime = DateTime.UtcNow.AddMinutes(-10),
                EndTime = null,
                Status = ConsultationStatus.Scheduled,
                Type = ConsultationType.Scheduled
            },
            new Consultation
            {
                Id = _completedConsultationId,
                CallerId = _memberId,
                CalleeId = _expertId,
                RoomId = "room-existing-report",
                StartTime = DateTime.UtcNow.AddHours(-3),
                EndTime = null,
                Status = ConsultationStatus.ExpertAbsent,
                Type = ConsultationType.Scheduled,
                CustomerReport = "Existing customer report",
                CustomerReportSubmittedAt = DateTime.UtcNow.AddHours(-2)
            },
            new Consultation
            {
                Id = _futureConsultationId,
                CallerId = _memberId,
                CalleeId = _expertId,
                RoomId = "room-future",
                StartTime = DateTime.UtcNow.AddHours(1),
                EndTime = null,
                Status = ConsultationStatus.Scheduled,
                Type = ConsultationType.Scheduled
            },
            new Consultation
            {
                Id = _emergencyConsultationId,
                CallerId = _memberId,
                CalleeId = _expertId,
                RoomId = "room-emergency",
                StartTime = DateTime.UtcNow.AddHours(-1),
                EndTime = null,
                Status = ConsultationStatus.Ongoing,
                Type = ConsultationType.Emergency
            });

        _db.Set<ConsultationBooking>().AddRange(
            new ConsultationBooking
            {
                Id = _bookingId,
                UserId = _memberId,
                ExpertId = _expertId,
                Price = 150_000m,
                BookedAt = DateTime.UtcNow.AddDays(-1),
                ProblemDescription = "Scheduled support",
                PaymentDeadline = DateTime.UtcNow.AddHours(-3),
                Status = BookingStatus.Confirmed,
                ConsultationId = _scheduledConsultationId,
                TimeSlotId = _slotId
            },
            new ConsultationBooking
            {
                Id = Guid.NewGuid(),
                UserId = _memberId,
                ExpertId = _expertId,
                Price = 150_000m,
                BookedAt = DateTime.UtcNow.AddDays(-2),
                ProblemDescription = "Existing report booking",
                PaymentDeadline = DateTime.UtcNow.AddHours(-4),
                Status = BookingStatus.Completed,
                ConsultationId = _completedConsultationId,
                TimeSlotId = _slotId
            },
            new ConsultationBooking
            {
                Id = Guid.NewGuid(),
                UserId = _memberId,
                ExpertId = _expertId,
                Price = 150_000m,
                BookedAt = DateTime.UtcNow,
                ProblemDescription = "Future booking",
                PaymentDeadline = DateTime.UtcNow.AddMinutes(30),
                Status = BookingStatus.Confirmed,
                ConsultationId = _futureConsultationId,
                TimeSlotId = _slotId
            });

        _db.Set<ConsultationPingRequest>().Add(new ConsultationPingRequest
        {
            Id = _pingRequestId,
            RescuerId = _memberId,
            ExpertId = _expertId,
            Status = ConsultationPingStatus.AcceptedByExpert,
            RequestedAt = DateTime.UtcNow.AddHours(-1),
            ConsultationId = _emergencyConsultationId
        });

        _db.SaveChanges();
    }

    private static Account CreateAccount(Guid id, string fullName, AccountRole role) =>
        new()
        {
            Id = id,
            FullName = fullName,
            UserName = $"{fullName.Replace(" ", ".").ToLowerInvariant()}.{id:N}",
            NormalizedUserName = $"{fullName.Replace(" ", ".").ToUpperInvariant()}.{id:N}",
            Email = $"{id:N}@test.local",
            NormalizedEmail = $"{id:N}@TEST.LOCAL",
            IsActive = true,
            Role = role
        };

    private sealed class ConsultationExpertAbsentSqliteDbContext : SnakeAidDbContext
    {
        public ConsultationExpertAbsentSqliteDbContext(DbContextOptions<SnakeAidDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var keep = new HashSet<Type>
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
                entity.Property(c => c.CustomerReport)
                    .HasMaxLength(2000);
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

            modelBuilder.Entity<ExpertTimeSlot>(entity =>
            {
                entity.HasKey(s => s.Id);
                entity.Property(s => s.Version)
                    .IsConcurrencyToken()
                    .ValueGeneratedNever()
                    .HasDefaultValue(0u);
                entity.HasOne(s => s.Expert)
                    .WithMany()
                    .HasForeignKey(s => s.ExpertId)
                    .OnDelete(DeleteBehavior.Restrict);
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
        public Task<Core.Responses.Consultation.ConsultationPaymentResponse> ConfirmConsultationPaymentAsync(Guid transactionId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Core.Responses.PayOs.PayOsWebhookResponse> ConfirmConsultationPaymentByOrderCodeAsync(long orderCode, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Core.Responses.Consultation.ConsultationPaymentResponse> PayScheduledBookingAsync(Guid userId, Guid bookingId, ProcessConsultationPaymentRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Core.Responses.Consultation.ConsultationPaymentResponse> PayEmergencyRequestAsync(Guid userId, Guid requestId, ProcessConsultationPaymentRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> IsConsultationPayOsOrderCodeAsync(long orderCode, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<Core.Responses.PayOs.PayOsWebhookResponse> ProcessConsultationWebhookAsync(string rawPayload, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> RefundEmergencyEscrowAsync(Guid requestId, string reason, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<bool> RefundScheduledBookingAsync(Guid bookingId, Guid receiverId, string reason, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<bool> CancelPendingScheduledBookingPaymentAsync(Guid bookingId, string reason, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<int> ExpireEmergencyRequestsAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<bool> SettleConsultationEscrowAsync(Guid consultationId, CancellationToken cancellationToken = default) => Task.FromResult(true);
    }
}
