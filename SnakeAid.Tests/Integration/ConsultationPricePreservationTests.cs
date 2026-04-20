using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Requests.Consultation;
using SnakeAid.Core.Responses.Consultation;
using SnakeAid.Core.Responses.PayOs;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Implements;
using SnakeAid.Service.Implements;
using SnakeAid.Service.Interfaces;
using System.Reflection;

namespace SnakeAid.Tests.Integration;

/// <summary>
/// Preservation property tests for GetMyConsultationsAsync.
/// These tests capture baseline behavior on UNFIXED code and must continue
/// to pass after the emergency-price fix is applied.
///
/// Property 2: Preservation - Scheduled Consultation Price, Filtering,
/// Sorting, Pagination Unchanged.
///
/// **Validates: Requirements 3.1, 3.2, 3.3, 3.4**
/// </summary>
public class ConsultationPricePreservationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SnakeAidDbContext _db;
    private readonly ConsultationService _service;

    // Shared IDs
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _expertId = Guid.NewGuid();

    // Scheduled consultation data (3 bookings with distinct prices and times)
    private readonly Guid _booking1Id = Guid.NewGuid();
    private readonly Guid _booking2Id = Guid.NewGuid();
    private readonly Guid _booking3Id = Guid.NewGuid();
    private readonly Guid _consultation1Id = Guid.NewGuid();
    private readonly Guid _consultation2Id = Guid.NewGuid();
    private readonly Guid _consultation3Id = Guid.NewGuid();
    private readonly Guid _slot1Id = Guid.NewGuid();
    private readonly Guid _slot2Id = Guid.NewGuid();
    private readonly Guid _slot3Id = Guid.NewGuid();

    // Emergency consultation data (2 emergency consultations, no transactions)
    private readonly Guid _emergencyConsultation1Id = Guid.NewGuid();
    private readonly Guid _emergencyConsultation2Id = Guid.NewGuid();
    private readonly Guid _pingRequest1Id = Guid.NewGuid();
    private readonly Guid _pingRequest2Id = Guid.NewGuid();

    // Known prices for scheduled consultations
    private const decimal Price1 = 100_000m;
    private const decimal Price2 = 250_000m;
    private const decimal Price3 = 500_000m;

    // Known start times (descending order: 3 > 2 > 1 > emergency2 > emergency1)
    private readonly DateTime _startTime1 = new(2025, 1, 10, 10, 0, 0, DateTimeKind.Utc);
    private readonly DateTime _startTime2 = new(2025, 1, 15, 14, 0, 0, DateTimeKind.Utc);
    private readonly DateTime _startTime3 = new(2025, 1, 20, 9, 0, 0, DateTimeKind.Utc);
    private readonly DateTime _emergencyStartTime1 = new(2025, 1, 5, 8, 0, 0, DateTimeKind.Utc);
    private readonly DateTime _emergencyStartTime2 = new(2025, 1, 8, 16, 0, 0, DateTimeKind.Utc);

    public ConsultationPricePreservationTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<SnakeAidDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new PreservationSqliteDbContext(options);
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

    private void SeedData()
    {
        // Seed user
        _db.Set<Account>().Add(new Account
        {
            Id = _userId,
            FullName = "Test User",
            UserName = $"user.{_userId:N}",
            NormalizedUserName = $"USER.{_userId:N}",
            Email = $"user.{_userId:N}@test.local",
            NormalizedEmail = $"USER.{_userId:N}@TEST.LOCAL",
            IsActive = true,
            Role = AccountRole.User
        });

        // Seed expert
        _db.Set<Account>().Add(new Account
        {
            Id = _expertId,
            FullName = "Test Expert",
            UserName = $"expert.{_expertId:N}",
            NormalizedUserName = $"EXPERT.{_expertId:N}",
            Email = $"expert.{_expertId:N}@test.local",
            NormalizedEmail = $"EXPERT.{_expertId:N}@TEST.LOCAL",
            IsActive = true,
            Role = AccountRole.Expert
        });

        // Seed 3 time slots
        _db.Set<ExpertTimeSlot>().AddRange(
            new ExpertTimeSlot { Id = _slot1Id, ExpertId = _expertId, StartTime = _startTime1, EndTime = _startTime1.AddHours(1), Status = TimeSlotStatus.Booked },
            new ExpertTimeSlot { Id = _slot2Id, ExpertId = _expertId, StartTime = _startTime2, EndTime = _startTime2.AddHours(1), Status = TimeSlotStatus.Booked },
            new ExpertTimeSlot { Id = _slot3Id, ExpertId = _expertId, StartTime = _startTime3, EndTime = _startTime3.AddHours(1), Status = TimeSlotStatus.Booked }
        );

        // Seed 3 scheduled consultations
        _db.Consultations.AddRange(
            new Consultation { Id = _consultation1Id, CallerId = _userId, CalleeId = _expertId, RoomId = "room-s1", StartTime = _startTime1, EndTime = _startTime1.AddHours(1), Status = ConsultationStatus.Completed, Type = ConsultationType.Scheduled },
            new Consultation { Id = _consultation2Id, CallerId = _userId, CalleeId = _expertId, RoomId = "room-s2", StartTime = _startTime2, EndTime = _startTime2.AddHours(1), Status = ConsultationStatus.Completed, Type = ConsultationType.Scheduled },
            new Consultation { Id = _consultation3Id, CallerId = _userId, CalleeId = _expertId, RoomId = "room-s3", StartTime = _startTime3, EndTime = _startTime3.AddHours(1), Status = ConsultationStatus.Completed, Type = ConsultationType.Scheduled }
        );

        // Seed 3 bookings with known prices
        _db.Set<ConsultationBooking>().AddRange(
            new ConsultationBooking { Id = _booking1Id, UserId = _userId, ExpertId = _expertId, Price = Price1, BookedAt = _startTime1.AddDays(-1), Status = BookingStatus.Completed, ConsultationId = _consultation1Id, TimeSlotId = _slot1Id, PaymentDeadline = _startTime1 },
            new ConsultationBooking { Id = _booking2Id, UserId = _userId, ExpertId = _expertId, Price = Price2, BookedAt = _startTime2.AddDays(-1), Status = BookingStatus.Completed, ConsultationId = _consultation2Id, TimeSlotId = _slot2Id, PaymentDeadline = _startTime2 },
            new ConsultationBooking { Id = _booking3Id, UserId = _userId, ExpertId = _expertId, Price = Price3, BookedAt = _startTime3.AddDays(-1), Status = BookingStatus.Completed, ConsultationId = _consultation3Id, TimeSlotId = _slot3Id, PaymentDeadline = _startTime3 }
        );

        // Seed 2 emergency consultations (no transactions — graceful degradation)
        _db.Consultations.AddRange(
            new Consultation { Id = _emergencyConsultation1Id, CallerId = _userId, CalleeId = _expertId, RoomId = "room-e1", StartTime = _emergencyStartTime1, EndTime = _emergencyStartTime1.AddMinutes(30), Status = ConsultationStatus.Completed, Type = ConsultationType.Emergency },
            new Consultation { Id = _emergencyConsultation2Id, CallerId = _userId, CalleeId = _expertId, RoomId = "room-e2", StartTime = _emergencyStartTime2, EndTime = _emergencyStartTime2.AddMinutes(30), Status = ConsultationStatus.Completed, Type = ConsultationType.Emergency }
        );

        _db.ConsultationPingRequests.AddRange(
            new ConsultationPingRequest { Id = _pingRequest1Id, RescuerId = _userId, ExpertId = _expertId, Status = ConsultationPingStatus.AcceptedByExpert, RequestedAt = _emergencyStartTime1, ConsultationId = _emergencyConsultation1Id },
            new ConsultationPingRequest { Id = _pingRequest2Id, RescuerId = _userId, ExpertId = _expertId, Status = ConsultationPingStatus.AcceptedByExpert, RequestedAt = _emergencyStartTime2, ConsultationId = _emergencyConsultation2Id }
        );

        _db.SaveChanges();
    }

    #region Scheduled Price Preservation

    /// <summary>
    /// Scheduled consultations must return Price == ConsultationBooking.Price.
    /// This is the core preservation property: the fix must not alter scheduled pricing.
    ///
    /// **Validates: Requirements 3.1**
    /// </summary>
    [Fact]
    public async Task ScheduledConsultations_PriceEqualsBookingPrice()
    {
        var query = new MyConsultationsQueryRequest { PageNumber = 1, PageSize = 10, Type = "Scheduled" };

        var result = await _service.GetMyConsultationsAsync(_userId, query);

        var items = result.Items.ToList();
        Assert.Equal(3, items.Count);

        // Map by BookingId and verify each price
        var byBooking = items.ToDictionary(i => i.BookingId!.Value);
        Assert.Equal(Price1, byBooking[_booking1Id].Price);
        Assert.Equal(Price2, byBooking[_booking2Id].Price);
        Assert.Equal(Price3, byBooking[_booking3Id].Price);
    }

    #endregion

    #region Type Filter Preservation

    /// <summary>
    /// Filtering by Type = "Scheduled" must return only scheduled consultations.
    ///
    /// **Validates: Requirements 3.2**
    /// </summary>
    [Fact]
    public async Task TypeFilter_Scheduled_ReturnsOnlyScheduled()
    {
        var query = new MyConsultationsQueryRequest { PageNumber = 1, PageSize = 10, Type = "Scheduled" };

        var result = await _service.GetMyConsultationsAsync(_userId, query);

        Assert.All(result.Items, item => Assert.Equal("Scheduled", item.Type));
        Assert.Equal(3, result.Items.Count());
    }

    /// <summary>
    /// Filtering by Type = "Emergency" must return only emergency consultations.
    ///
    /// **Validates: Requirements 3.2**
    /// </summary>
    [Fact]
    public async Task TypeFilter_Emergency_ReturnsOnlyEmergency()
    {
        var query = new MyConsultationsQueryRequest { PageNumber = 1, PageSize = 10, Type = "Emergency" };

        var result = await _service.GetMyConsultationsAsync(_userId, query);

        Assert.All(result.Items, item => Assert.Equal("Emergency", item.Type));
        Assert.Equal(2, result.Items.Count());
    }

    #endregion

    #region Sort Order Preservation

    /// <summary>
    /// Results must be sorted by StartTime descending.
    /// Expected order: consultation3 (Jan 20) > consultation2 (Jan 15) > consultation1 (Jan 10)
    ///                 > emergency2 (Jan 8) > emergency1 (Jan 5)
    ///
    /// **Validates: Requirements 3.4**
    /// </summary>
    [Fact]
    public async Task Results_SortedByStartTimeDescending()
    {
        var query = new MyConsultationsQueryRequest { PageNumber = 1, PageSize = 10 };

        var result = await _service.GetMyConsultationsAsync(_userId, query);

        var items = result.Items.ToList();
        Assert.Equal(5, items.Count);

        // Verify descending order
        for (int i = 0; i < items.Count - 1; i++)
        {
            Assert.True(
                items[i].StartTime >= items[i + 1].StartTime,
                $"Item at index {i} (StartTime={items[i].StartTime}) should be >= item at index {i + 1} (StartTime={items[i + 1].StartTime})");
        }

        // Verify exact order by ConsultationId
        Assert.Equal(_consultation3Id, items[0].ConsultationId);  // Jan 20
        Assert.Equal(_consultation2Id, items[1].ConsultationId);  // Jan 15
        Assert.Equal(_consultation1Id, items[2].ConsultationId);  // Jan 10
        Assert.Equal(_emergencyConsultation2Id, items[3].ConsultationId); // Jan 8
        Assert.Equal(_emergencyConsultation1Id, items[4].ConsultationId); // Jan 5
    }

    #endregion

    #region Pagination Preservation

    /// <summary>
    /// Pagination metadata must be correct when results span multiple pages.
    /// With 5 total items and PageSize=2, we expect 3 pages.
    ///
    /// **Validates: Requirements 3.4**
    /// </summary>
    [Fact]
    public async Task Pagination_MetadataIsCorrect()
    {
        var query = new MyConsultationsQueryRequest { PageNumber = 1, PageSize = 2 };

        var result = await _service.GetMyConsultationsAsync(_userId, query);

        Assert.Equal(5, result.Meta.TotalItems);
        Assert.Equal(3, result.Meta.TotalPages);
        Assert.Equal(1, result.Meta.CurrentPage);
        Assert.Equal(2, result.Meta.PageSize);
        Assert.Equal(2, result.Items.Count());
    }

    /// <summary>
    /// Page 2 should return the next 2 items in sort order.
    ///
    /// **Validates: Requirements 3.4**
    /// </summary>
    [Fact]
    public async Task Pagination_Page2_ReturnsCorrectItems()
    {
        var query = new MyConsultationsQueryRequest { PageNumber = 2, PageSize = 2 };

        var result = await _service.GetMyConsultationsAsync(_userId, query);

        var items = result.Items.ToList();
        Assert.Equal(2, items.Count);
        // Sorted descending: page 2 should have items at index 2 and 3
        // consultation1 (Jan 10) and emergency2 (Jan 8)
        Assert.Equal(_consultation1Id, items[0].ConsultationId);
        Assert.Equal(_emergencyConsultation2Id, items[1].ConsultationId);
        Assert.Equal(2, result.Meta.CurrentPage);
    }

    /// <summary>
    /// Last page should return remaining items.
    ///
    /// **Validates: Requirements 3.4**
    /// </summary>
    [Fact]
    public async Task Pagination_LastPage_ReturnsRemainingItems()
    {
        var query = new MyConsultationsQueryRequest { PageNumber = 3, PageSize = 2 };

        var result = await _service.GetMyConsultationsAsync(_userId, query);

        var items = result.Items.ToList();
        Assert.Single(items);
        Assert.Equal(_emergencyConsultation1Id, items[0].ConsultationId);
        Assert.Equal(3, result.Meta.CurrentPage);
    }

    #endregion

    #region Graceful Degradation

    /// <summary>
    /// Emergency consultations with no matching Transaction should return Price = null.
    /// This tests graceful degradation — no crash, just null price.
    ///
    /// **Validates: Requirements 3.3**
    /// </summary>
    [Fact]
    public async Task EmergencyConsultation_NoTransaction_PriceIsNull()
    {
        var query = new MyConsultationsQueryRequest { PageNumber = 1, PageSize = 10, Type = "Emergency" };

        var result = await _service.GetMyConsultationsAsync(_userId, query);

        var items = result.Items.ToList();
        Assert.Equal(2, items.Count);
        Assert.All(items, item =>
        {
            Assert.Equal("Emergency", item.Type);
            Assert.Null(item.Price);
        });
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Minimal SQLite DbContext that only keeps the entities needed for these tests.
    /// Same pattern as ConsultationPriceBugConditionTests.
    /// </summary>
    private sealed class PreservationSqliteDbContext : SnakeAidDbContext
    {
        public PreservationSqliteDbContext(DbContextOptions<SnakeAidDbContext> options)
            : base(options) { }

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
                    modelBuilder.Ignore(type);
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

    #endregion
}
