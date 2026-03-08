using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Requests.Consultation;
using SnakeAid.Core.Responses.Consultation;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Implements;
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

        var bookingService = new BookingService(new UnitOfWork<SnakeAidDbContext>(db), new FakeConsultationPaymentService(), NullLogger<BookingService>.Instance);
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

        public Task<ConsultationPaymentResponse> PayScheduledBookingAsync(Guid userId, Guid bookingId, ProcessConsultationPaymentRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<ConsultationPaymentResponse> PayEmergencyRequestAsync(Guid userId, Guid requestId, ProcessConsultationPaymentRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> RefundEmergencyEscrowAsync(Guid requestId, string reason, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<int> ExpireEmergencyRequestsAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<bool> SettleConsultationEscrowAsync(Guid consultationId, CancellationToken cancellationToken = default)
        {
            SettledConsultationIds.Add(consultationId);
            return Task.FromResult(true);
        }
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
