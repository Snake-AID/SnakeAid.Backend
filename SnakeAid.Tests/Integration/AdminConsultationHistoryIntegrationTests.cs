using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Requests.Consultation;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Implements;
using SnakeAid.Service.Implements;
using SnakeAid.Service.Interfaces;
using System.Reflection;

namespace SnakeAid.Tests.Integration;

public class AdminConsultationHistoryIntegrationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SnakeAidDbContext _db;
    private readonly ConsultationService _service;

    private readonly Guid _user1Id = Guid.NewGuid();
    private readonly Guid _user2Id = Guid.NewGuid();
    private readonly Guid _expert1Id = Guid.NewGuid();
    private readonly Guid _expert2Id = Guid.NewGuid();

    private readonly Guid _scheduledConsultation1Id = Guid.NewGuid();
    private readonly Guid _scheduledConsultation2Id = Guid.NewGuid();
    private readonly Guid _orphanScheduledConsultationId = Guid.NewGuid();
    private readonly Guid _emergencyConsultation1Id = Guid.NewGuid();
    private readonly Guid _emergencyConsultation2Id = Guid.NewGuid();

    private readonly Guid _booking1Id = Guid.NewGuid();
    private readonly Guid _booking2Id = Guid.NewGuid();
    private readonly Guid _slot1Id = Guid.NewGuid();
    private readonly Guid _slot2Id = Guid.NewGuid();
    private readonly Guid _pingRequest1Id = Guid.NewGuid();
    private readonly Guid _pingRequest2Id = Guid.NewGuid();

    public AdminConsultationHistoryIntegrationTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<SnakeAidDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new AdminConsultationHistorySqliteDbContext(options);
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
    public async Task GetAllConsultationsForAdminAsync_ShouldReturnMergedResultsSortedByStartTimeDescending()
    {
        var result = await _service.GetAllConsultationsForAdminAsync(new AdminConsultationsQueryRequest
        {
            PageNumber = 1,
            PageSize = 10
        });

        var items = result.Items.ToList();

        Assert.Equal(5, result.Meta.TotalItems);
        Assert.Equal(1, result.Meta.TotalPages);
        Assert.Equal(_emergencyConsultation1Id, items[0].ConsultationId);
        Assert.Equal(_scheduledConsultation1Id, items[1].ConsultationId);
        Assert.Equal(_orphanScheduledConsultationId, items[2].ConsultationId);
        Assert.Equal(_emergencyConsultation2Id, items[3].ConsultationId);
        Assert.Equal(_scheduledConsultation2Id, items[4].ConsultationId);
    }

    [Fact]
    public async Task GetAllConsultationsForAdminAsync_ShouldMapScheduledConsultationFields()
    {
        var result = await _service.GetAllConsultationsForAdminAsync(new AdminConsultationsQueryRequest
        {
            PageNumber = 1,
            PageSize = 10,
            Type = "Scheduled",
            Status = "Completed"
        });

        Assert.Equal(2, result.Meta.TotalItems);
        var item = Assert.Single(result.Items, consultation => consultation.BookingId == _booking1Id);
        Assert.Equal(_scheduledConsultation1Id, item.ConsultationId);
        Assert.Equal(_user1Id, item.UserId);
        Assert.Equal("Member One", item.UserName);
        Assert.Equal(_expert1Id, item.ExpertId);
        Assert.Equal("Expert One", item.ExpertName);
        Assert.Equal(150_000m, item.Price);
        Assert.Equal("Snakebite on arm", item.ProblemDescription);
        Assert.Equal(_booking1Id, item.BookingId);
        Assert.Null(item.EmergencyRequestId);
        Assert.Equal(new DateTime(2026, 4, 9, 8, 0, 0, DateTimeKind.Utc), item.SlotStartTime);
        Assert.Equal(new DateTime(2026, 4, 9, 8, 30, 0, DateTimeKind.Utc), item.SlotEndTime);
    }

    [Fact]
    public async Task GetAllConsultationsForAdminAsync_ShouldPreferConsultationPaymentAndFallbackToExpertPayout()
    {
        var result = await _service.GetAllConsultationsForAdminAsync(new AdminConsultationsQueryRequest
        {
            PageNumber = 1,
            PageSize = 10,
            Type = "Emergency"
        });

        var items = result.Items.OrderBy(i => i.ConsultationId).ToList();
        Assert.Equal(2, items.Count);

        var paidByUser = items.Single(i => i.ConsultationId == _emergencyConsultation1Id);
        var paidByFallback = items.Single(i => i.ConsultationId == _emergencyConsultation2Id);

        Assert.Equal(220_000m, paidByUser.Price);
        Assert.Equal(_pingRequest1Id, paidByUser.EmergencyRequestId);
        Assert.Equal(180_000m, paidByFallback.Price);
        Assert.Equal(_pingRequest2Id, paidByFallback.EmergencyRequestId);
    }

    [Fact]
    public async Task GetAllConsultationsForAdminAsync_ShouldFilterAndPaginateCorrectly()
    {
        var completedPage1 = await _service.GetAllConsultationsForAdminAsync(new AdminConsultationsQueryRequest
        {
            PageNumber = 1,
            PageSize = 2,
            Status = "Completed"
        });

        var completedPage2 = await _service.GetAllConsultationsForAdminAsync(new AdminConsultationsQueryRequest
        {
            PageNumber = 2,
            PageSize = 2,
            Status = "Completed"
        });

        Assert.Equal(4, completedPage1.Meta.TotalItems);
        Assert.Equal(2, completedPage1.Meta.TotalPages);
        Assert.Equal(2, completedPage1.Items.Count());
        Assert.Equal(2, completedPage2.Items.Count());
        Assert.All(completedPage1.Items.Concat(completedPage2.Items), item => Assert.Equal("Completed", item.Status));
    }

    [Fact]
    public async Task GetAllConsultationsForAdminAsync_InvalidType_ShouldThrowArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GetAllConsultationsForAdminAsync(
            new AdminConsultationsQueryRequest
            {
                PageNumber = 1,
                PageSize = 10,
                Type = "InvalidType"
            }));
    }

    private void SeedData()
    {
        _db.Set<Account>().AddRange(
            CreateAccount(_user1Id, "Member One", AccountRole.User),
            CreateAccount(_user2Id, "Member Two", AccountRole.User),
            CreateAccount(_expert1Id, "Expert One", AccountRole.Expert),
            CreateAccount(_expert2Id, "Expert Two", AccountRole.Expert));

        _db.Set<ExpertTimeSlot>().AddRange(
            new ExpertTimeSlot
            {
                Id = _slot1Id,
                ExpertId = _expert1Id,
                StartTime = new DateTime(2026, 4, 9, 8, 0, 0, DateTimeKind.Utc),
                EndTime = new DateTime(2026, 4, 9, 8, 30, 0, DateTimeKind.Utc),
                Status = TimeSlotStatus.Booked
            },
            new ExpertTimeSlot
            {
                Id = _slot2Id,
                ExpertId = _expert2Id,
                StartTime = new DateTime(2026, 4, 7, 8, 0, 0, DateTimeKind.Utc),
                EndTime = new DateTime(2026, 4, 7, 8, 30, 0, DateTimeKind.Utc),
                Status = TimeSlotStatus.Booked
            });

        _db.Set<Consultation>().AddRange(
            new Consultation
            {
                Id = _scheduledConsultation1Id,
                CallerId = _user1Id,
                CalleeId = _expert1Id,
                RoomId = "room-scheduled-1",
                StartTime = new DateTime(2026, 4, 9, 8, 0, 0, DateTimeKind.Utc),
                EndTime = new DateTime(2026, 4, 9, 8, 30, 0, DateTimeKind.Utc),
                Status = ConsultationStatus.Completed,
                Type = ConsultationType.Scheduled
            },
            new Consultation
            {
                Id = _scheduledConsultation2Id,
                CallerId = _user2Id,
                CalleeId = _expert2Id,
                RoomId = "room-scheduled-2",
                StartTime = new DateTime(2026, 4, 7, 8, 0, 0, DateTimeKind.Utc),
                EndTime = null,
                Status = ConsultationStatus.Ongoing,
                Type = ConsultationType.Scheduled
            },
            new Consultation
            {
                Id = _orphanScheduledConsultationId,
                CallerId = _user2Id,
                CalleeId = _expert1Id,
                RoomId = "room-scheduled-orphan",
                StartTime = new DateTime(2026, 4, 8, 12, 0, 0, DateTimeKind.Utc),
                EndTime = new DateTime(2026, 4, 8, 12, 30, 0, DateTimeKind.Utc),
                Status = ConsultationStatus.Completed,
                Type = ConsultationType.Scheduled
            },
            new Consultation
            {
                Id = _emergencyConsultation1Id,
                CallerId = _user1Id,
                CalleeId = _expert2Id,
                RoomId = "room-emergency-1",
                StartTime = new DateTime(2026, 4, 10, 9, 0, 0, DateTimeKind.Utc),
                EndTime = new DateTime(2026, 4, 10, 9, 25, 0, DateTimeKind.Utc),
                Status = ConsultationStatus.Completed,
                Type = ConsultationType.Emergency
            },
            new Consultation
            {
                Id = _emergencyConsultation2Id,
                CallerId = _user2Id,
                CalleeId = _expert1Id,
                RoomId = "room-emergency-2",
                StartTime = new DateTime(2026, 4, 8, 11, 0, 0, DateTimeKind.Utc),
                EndTime = new DateTime(2026, 4, 8, 11, 15, 0, DateTimeKind.Utc),
                Status = ConsultationStatus.Completed,
                Type = ConsultationType.Emergency
            });

        _db.Set<ConsultationBooking>().AddRange(
            new ConsultationBooking
            {
                Id = _booking1Id,
                UserId = _user1Id,
                ExpertId = _expert1Id,
                Price = 150_000m,
                BookedAt = new DateTime(2026, 4, 8, 8, 0, 0, DateTimeKind.Utc),
                ProblemDescription = "Snakebite on arm",
                PaymentDeadline = new DateTime(2026, 4, 9, 7, 45, 0, DateTimeKind.Utc),
                Status = BookingStatus.Completed,
                ConsultationId = _scheduledConsultation1Id,
                TimeSlotId = _slot1Id
            },
            new ConsultationBooking
            {
                Id = _booking2Id,
                UserId = _user2Id,
                ExpertId = _expert2Id,
                Price = 90_000m,
                BookedAt = new DateTime(2026, 4, 6, 8, 0, 0, DateTimeKind.Utc),
                ProblemDescription = "Follow-up question",
                PaymentDeadline = new DateTime(2026, 4, 7, 7, 45, 0, DateTimeKind.Utc),
                Status = BookingStatus.Confirmed,
                ConsultationId = _scheduledConsultation2Id,
                TimeSlotId = _slot2Id
            });

        _db.Set<ConsultationPingRequest>().AddRange(
            new ConsultationPingRequest
            {
                Id = _pingRequest1Id,
                RescuerId = _user1Id,
                ExpertId = _expert2Id,
                Status = ConsultationPingStatus.AcceptedByExpert,
                RequestedAt = new DateTime(2026, 4, 10, 8, 55, 0, DateTimeKind.Utc),
                ConsultationId = _emergencyConsultation1Id
            },
            new ConsultationPingRequest
            {
                Id = _pingRequest2Id,
                RescuerId = _user2Id,
                ExpertId = _expert1Id,
                Status = ConsultationPingStatus.AcceptedByExpert,
                RequestedAt = new DateTime(2026, 4, 8, 10, 55, 0, DateTimeKind.Utc),
                ConsultationId = _emergencyConsultation2Id
            });

        _db.Set<Transaction>().AddRange(
            new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = _user1Id,
                ReferenceId = _pingRequest1Id,
                Amount = 220_000m,
                Currency = "VND",
                TransactionType = TransactionType.ConsultationPayment,
                CreatedAt = new DateTime(2026, 4, 10, 8, 56, 0, DateTimeKind.Utc)
            },
            new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = _expert1Id,
                ReferenceId = _emergencyConsultation2Id,
                Amount = 180_000m,
                Currency = "VND",
                TransactionType = TransactionType.ExpertPayout,
                CreatedAt = new DateTime(2026, 4, 8, 11, 20, 0, DateTimeKind.Utc)
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

    private sealed class AdminConsultationHistorySqliteDbContext : SnakeAidDbContext
    {
        public AdminConsultationHistorySqliteDbContext(DbContextOptions<SnakeAidDbContext> options)
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
        public Task<int> ExpireEmergencyRequestsAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<bool> SettleConsultationEscrowAsync(Guid consultationId, CancellationToken cancellationToken = default) => Task.FromResult(true);
    }
}
