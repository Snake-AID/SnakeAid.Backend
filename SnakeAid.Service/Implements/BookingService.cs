using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Constants;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Requests.Consultation;
using SnakeAid.Core.Responses.Consultation;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Hubs;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Service.Implements;

public class BookingService : IBookingService
{
    private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
    private readonly IConsultationPaymentService _consultationPaymentService;
    private readonly IHubContext<ConsultationHub> _hubContext;
    private readonly ILiveKitService _liveKitService;
    private readonly ILogger<BookingService> _logger;

    public BookingService(
        IUnitOfWork<SnakeAidDbContext> unitOfWork,
        IConsultationPaymentService consultationPaymentService,
        IHubContext<ConsultationHub> hubContext,
        ILiveKitService liveKitService,
        ILogger<BookingService> logger)
    {
        _unitOfWork = unitOfWork;
        _consultationPaymentService = consultationPaymentService;
        _hubContext = hubContext;
        _liveKitService = liveKitService;
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
                    UserName = null,
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
            UserName = booking.User?.FullName,
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

    public async Task<IEnumerable<ConsultationBookingResponse>> GetExpertBookingsAsync(Guid expertId)
    {
        var bookings = await _unitOfWork.GetRepository<ConsultationBooking>().GetListAsync(
            predicate: b =>
                b.ExpertId == expertId
                && (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Completed),
            orderBy: q => q.OrderBy(b => b.TimeSlot.StartTime),
            include: q => q
                .Include(b => b.User)
                .Include(b => b.Expert)
                .Include(b => b.TimeSlot)
                .Include(b => b.Consultation));

        return bookings.Select(booking => new ConsultationBookingResponse
        {
            Id = booking.Id,
            UserId = booking.UserId,
            UserName = booking.User?.FullName,
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
            var consultationId = booking.ConsultationId!.Value;
            var roomName = $"consultation-{consultationId}";

            try
            {
                // Step 1: Send ConsultationCallEnded signal via SignalR (best-effort)
                try
                {
                    await _hubContext.Clients.Group($"consultation:{consultationId}")
                        .SendAsync(ConsultationRealtimeEvents.ConsultationCallEnded, new
                        {
                            ConsultationId = consultationId,
                            Reason = ConsultationRealtimeEvents.ConsultationCallEndReasons.Timeout
                        }, cancellationToken);

                    _logger.LogInformation(
                        "Sent ConsultationCallEnded signal for consultation {ConsultationId}, RoomId={RoomId}, StartTime={StartTime}, ExpiryAction={ExpiryAction}",
                        consultationId, roomName, booking.Consultation?.StartTime, "room_expiring_signal_sent");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to send ConsultationCallEnded signal for consultation {ConsultationId}, RoomId={RoomId}, StartTime={StartTime}, ExpiryAction={ExpiryAction}",
                        consultationId, roomName, booking.Consultation?.StartTime, "room_expiring_signal_failed");
                }

                // Step 2: Delete LiveKit room (log error and continue if fails)
                try
                {
                    await _liveKitService.DeleteRoomAsync(roomName, cancellationToken);

                    _logger.LogInformation(
                        "Deleted LiveKit room for consultation {ConsultationId}, RoomId={RoomId}, StartTime={StartTime}, ExpiryAction={ExpiryAction}",
                        consultationId, roomName, booking.Consultation?.StartTime, "room_deleted");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to delete LiveKit room for consultation {ConsultationId}, RoomId={RoomId}, StartTime={StartTime}, ExpiryAction={ExpiryAction}",
                        consultationId, roomName, booking.Consultation?.StartTime, "room_deletion_failed");
                }

                // Step 3: Update BookingStatus = Completed
                booking.Status = BookingStatus.Completed;
                _unitOfWork.GetRepository<ConsultationBooking>().Update(booking);

                // Step 4: Update Consultation.Status = Completed, EndTime = SlotEndTime
                if (booking.Consultation != null)
                {
                    booking.Consultation.Status = ConsultationStatus.Completed;
                    booking.Consultation.EndTime = booking.TimeSlot.EndTime;
                    _unitOfWork.GetRepository<Consultation>().Update(booking.Consultation);
                }

                _logger.LogInformation(
                    "Updated status for consultation {ConsultationId}, RoomId={RoomId}, StartTime={StartTime}, ExpiryAction={ExpiryAction}",
                    consultationId, roomName, booking.Consultation?.StartTime, "status_updated");

                // Step 5: Update TimeSlot.Status = Booked if Reserved
                if (booking.TimeSlot.Status == TimeSlotStatus.Reserved)
                {
                    booking.TimeSlot.Status = TimeSlotStatus.Booked;
                    _unitOfWork.GetRepository<ExpertTimeSlot>().Update(booking.TimeSlot);
                }

                // Step 6: CommitAsync
                await _unitOfWork.CommitAsync();

                // Step 7: SettleConsultationEscrowAsync
                await _consultationPaymentService.SettleConsultationEscrowAsync(consultationId, cancellationToken);

                _logger.LogInformation(
                    "Settlement triggered for consultation {ConsultationId}, RoomId={RoomId}, StartTime={StartTime}, ExpiryAction={ExpiryAction}",
                    consultationId, roomName, booking.Consultation?.StartTime, "settlement_triggered");

                completedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error processing auto-complete for consultation {ConsultationId}, RoomId={RoomId}, StartTime={StartTime}, ExpiryAction={ExpiryAction}",
                    consultationId, roomName, booking.Consultation?.StartTime, "auto_complete_failed");
            }
        }

        _logger.LogInformation("Auto-complete scheduled consultations sweep completed. Total rooms processed: {CompletedCount}", completedCount);

        return completedCount;
    }

    public async Task<int> AutoCompleteElapsedEmergencyConsultationsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var expiryThreshold = now.AddMinutes(-30);

        var consultations = await _unitOfWork.GetRepository<Consultation>().GetListAsync(
            predicate: c =>
                c.Status == ConsultationStatus.Ongoing
                && c.Type == ConsultationType.Emergency
                && c.StartTime <= expiryThreshold,
            asNoTracking: false,
            cancellationToken: cancellationToken);

        var completedCount = 0;
        foreach (var consultation in consultations)
        {
            var roomName = $"consultation-{consultation.Id}";

            try
            {
                // Step 1: Send ConsultationCallEnded signal via SignalR (best-effort)
                try
                {
                    await _hubContext.Clients.Group($"consultation:{consultation.Id}")
                        .SendAsync(ConsultationRealtimeEvents.ConsultationCallEnded, new
                        {
                            ConsultationId = consultation.Id,
                            Reason = ConsultationRealtimeEvents.ConsultationCallEndReasons.Timeout
                        }, cancellationToken);

                    _logger.LogInformation(
                        "Sent ConsultationCallEnded signal for emergency consultation {ConsultationId}, RoomId={RoomId}, StartTime={StartTime}, ExpiryAction={ExpiryAction}",
                        consultation.Id, roomName, consultation.StartTime, "room_expiring_signal_sent");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to send ConsultationCallEnded signal for emergency consultation {ConsultationId}, RoomId={RoomId}, StartTime={StartTime}, ExpiryAction={ExpiryAction}",
                        consultation.Id, roomName, consultation.StartTime, "room_expiring_signal_failed");
                }

                // Step 2: Delete LiveKit room (log error and continue if fails)
                try
                {
                    await _liveKitService.DeleteRoomAsync(roomName, cancellationToken);

                    _logger.LogInformation(
                        "Deleted LiveKit room for emergency consultation {ConsultationId}, RoomId={RoomId}, StartTime={StartTime}, ExpiryAction={ExpiryAction}",
                        consultation.Id, roomName, consultation.StartTime, "room_deleted");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to delete LiveKit room for emergency consultation {ConsultationId}, RoomId={RoomId}, StartTime={StartTime}, ExpiryAction={ExpiryAction}",
                        consultation.Id, roomName, consultation.StartTime, "room_deletion_failed");
                }

                // Step 3: Update Consultation.Status = Completed, EndTime = UtcNow
                consultation.Status = ConsultationStatus.Completed;
                consultation.EndTime = DateTime.UtcNow;
                _unitOfWork.GetRepository<Consultation>().Update(consultation);

                _logger.LogInformation(
                    "Updated status for emergency consultation {ConsultationId}, RoomId={RoomId}, StartTime={StartTime}, ExpiryAction={ExpiryAction}",
                    consultation.Id, roomName, consultation.StartTime, "status_updated");

                // Step 4: CommitAsync
                await _unitOfWork.CommitAsync();

                // Step 5: SettleConsultationEscrowAsync
                await _consultationPaymentService.SettleConsultationEscrowAsync(consultation.Id, cancellationToken);

                _logger.LogInformation(
                    "Settlement triggered for emergency consultation {ConsultationId}, RoomId={RoomId}, StartTime={StartTime}, ExpiryAction={ExpiryAction}",
                    consultation.Id, roomName, consultation.StartTime, "settlement_triggered");

                completedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error processing auto-complete for emergency consultation {ConsultationId}, RoomId={RoomId}, StartTime={StartTime}, ExpiryAction={ExpiryAction}",
                    consultation.Id, roomName, consultation.StartTime, "auto_complete_failed");
            }
        }

        _logger.LogInformation("Auto-complete emergency consultations sweep completed. Total rooms processed: {CompletedCount}", completedCount);

        return completedCount;
    }
}
