using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Requests.Consultation;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Implements;
using SnakeAid.Service.Implements;

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

        var bookingService = new BookingService(new UnitOfWork<SnakeAidDbContext>(db), NullLogger<BookingService>.Instance);
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

        var consultationService = new ConsultationService(new UnitOfWork<SnakeAidDbContext>(db), NullLogger<ConsultationService>.Instance);
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

        var consultationService = new ConsultationService(new UnitOfWork<SnakeAidDbContext>(db), NullLogger<ConsultationService>.Instance);
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

    private static SnakeAidDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<SnakeAidDbContext>()
            .UseInMemoryDatabase($"ScheduledConsultationTests_{Guid.NewGuid():N}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new SnakeAidDbContext(options);
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
}
