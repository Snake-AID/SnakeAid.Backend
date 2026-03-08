using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Requests.Consultation;
using SnakeAid.Core.Responses.Consultation;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Service.Implements;

public class BookingService : IBookingService
{
    private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
    private readonly IConsultationPaymentService _consultationPaymentService;
    private readonly ILogger<BookingService> _logger;

    public BookingService(
        IUnitOfWork<SnakeAidDbContext> unitOfWork,
        IConsultationPaymentService consultationPaymentService,
        ILogger<BookingService> logger)
    {
        _unitOfWork = unitOfWork;
        _consultationPaymentService = consultationPaymentService;
        _logger = logger;
    }

    public async Task<ConsultationBookingResponse> CreateScheduledBookingAsync(Guid userId, CreateConsultationBookingRequest request)
    {
        try
        {
            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var timeSlotRepo = _unitOfWork.GetRepository<ExpertTimeSlot>();
                var slot = await timeSlotRepo.FirstOrDefaultAsync(
                    predicate: s => s.Id == request.TimeSlotId,
                    asNoTracking: false);

                if (slot == null)
                {
                    throw new NotFoundException("Requested time slot was not found.");
                }

                if (slot.StartTime <= DateTime.UtcNow)
                {
                    throw new ConflictException("This time slot has already started and can no longer be booked.");
                }

                if (slot.Status != TimeSlotStatus.Available)
                {
                    throw new ConflictException("This time slot is no longer available.");
                }

                var expertProfile = await _unitOfWork.GetRepository<ExpertProfile>().FirstOrDefaultAsync(
                    predicate: p => p.AccountId == slot.ExpertId);

                if (expertProfile == null)
                {
                    throw new NotFoundException("Expert profile not found for the selected slot.");
                }

                var consultationId = Guid.NewGuid();
                var consultation = new Consultation
                {
                    Id = consultationId,
                    CallerId = userId,
                    CalleeId = slot.ExpertId,
                    RoomId = $"consultation-{consultationId}",
                    StartTime = slot.StartTime,
                    EndTime = null,
                    Status = ConsultationStatus.Scheduled,
                    Type = ConsultationType.Scheduled
                };

                var booking = new ConsultationBooking
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ExpertId = slot.ExpertId,
                    Price = expertProfile.ConsultationFee,
                    BookedAt = DateTime.UtcNow,
                    PaymentDeadline = DateTime.UtcNow.AddMinutes(15),
                    Status = BookingStatus.PendingPayment,
                    TimeSlotId = slot.Id,
                    ConsultationId = consultationId,
                    ProblemDescription = request.ProblemDescription
                };

                slot.Status = TimeSlotStatus.Reserved;
                timeSlotRepo.Update(slot);

                await _unitOfWork.GetRepository<Consultation>().InsertAsync(consultation);
                await _unitOfWork.GetRepository<ConsultationBooking>().InsertAsync(booking);

                var expertAccount = await _unitOfWork.GetRepository<Account>().FirstOrDefaultAsync(
                    predicate: a => a.Id == slot.ExpertId);

                return new ConsultationBookingResponse
                {
                    Id = booking.Id,
                    UserId = booking.UserId,
                    ExpertId = booking.ExpertId,
                    ExpertName = expertAccount?.FullName,
                    Price = booking.Price,
                    BookedAt = booking.BookedAt,
                    PaymentDeadline = booking.PaymentDeadline,
                    Status = booking.Status,
                    ProblemDescription = booking.ProblemDescription,
                    TimeSlotId = booking.TimeSlotId,
                    SlotStartTime = slot.StartTime,
                    SlotEndTime = slot.EndTime,
                    ConsultationId = booking.ConsultationId,
                    RoomId = consultation.RoomId
                };
            });
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Booking concurrency conflict for userId={UserId}, timeSlotId={TimeSlotId}", userId, request.TimeSlotId);
            throw new ConflictException("This time slot was booked by another request. Please refresh and try again.");
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Unexpected database update error while creating booking for userId={UserId}, timeSlotId={TimeSlotId}", userId, request.TimeSlotId);
            throw;
        }
    }

    public async Task<IEnumerable<ConsultationBookingResponse>> GetMyBookingsAsync(Guid userId)
    {
        var bookings = await _unitOfWork.GetRepository<ConsultationBooking>().GetListAsync(
            predicate: b => b.UserId == userId,
            orderBy: q => q.OrderByDescending(b => b.BookedAt),
            include: q => q.Include(b => b.Expert).Include(b => b.TimeSlot).Include(b => b.Consultation));

        return bookings.Select(booking => new ConsultationBookingResponse
        {
            Id = booking.Id,
            UserId = booking.UserId,
            ExpertId = booking.ExpertId,
            ExpertName = booking.Expert?.FullName,
            Price = booking.Price,
            BookedAt = booking.BookedAt,
            PaymentDeadline = booking.PaymentDeadline,
            Status = booking.Status,
            ProblemDescription = booking.ProblemDescription,
            TimeSlotId = booking.TimeSlotId,
            SlotStartTime = booking.TimeSlot.StartTime,
            SlotEndTime = booking.TimeSlot.EndTime,
            ConsultationId = booking.ConsultationId,
            RoomId = booking.Consultation?.RoomId
        });
    }

    public async Task<int> AutoCompleteElapsedScheduledConsultationsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var bookings = await _unitOfWork.GetRepository<ConsultationBooking>().GetListAsync(
            predicate: b =>
                b.Status == BookingStatus.Confirmed
                && b.ConsultationId.HasValue
                && b.TimeSlot.EndTime <= now
                && b.Consultation != null
                && b.Consultation.Status != ConsultationStatus.Completed,
            include: q => q.Include(b => b.TimeSlot).Include(b => b.Consultation),
            asNoTracking: false,
            cancellationToken: cancellationToken);

        var completedCount = 0;
        foreach (var booking in bookings)
        {
            booking.Status = BookingStatus.Completed;
            _unitOfWork.GetRepository<ConsultationBooking>().Update(booking);

            if (booking.Consultation != null)
            {
                booking.Consultation.Status = ConsultationStatus.Completed;
                booking.Consultation.EndTime = booking.TimeSlot.EndTime;
                _unitOfWork.GetRepository<Consultation>().Update(booking.Consultation);
            }

            if (booking.TimeSlot.Status == TimeSlotStatus.Reserved)
            {
                booking.TimeSlot.Status = TimeSlotStatus.Booked;
                _unitOfWork.GetRepository<ExpertTimeSlot>().Update(booking.TimeSlot);
            }

            await _unitOfWork.CommitAsync();

            if (booking.ConsultationId.HasValue)
            {
                await _consultationPaymentService.SettleConsultationEscrowAsync(booking.ConsultationId.Value, cancellationToken);
            }

            completedCount++;
        }

        return completedCount;
    }
}
