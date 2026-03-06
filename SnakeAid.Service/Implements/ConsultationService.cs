using Microsoft.Extensions.Logging;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Requests.Consultation;
using SnakeAid.Core.Responses.UserFeedback;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Service.Implements;

public class ConsultationService : IConsultationService
{
    private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
    private readonly ILogger<ConsultationService> _logger;

    public ConsultationService(IUnitOfWork<SnakeAidDbContext> unitOfWork, ILogger<ConsultationService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task EndConsultationAsync(Guid consultationId, Guid actorId)
    {
        var consultationRepo = _unitOfWork.GetRepository<Consultation>();
        var consultation = await consultationRepo.FirstOrDefaultAsync(
            predicate: c => c.Id == consultationId,
            asNoTracking: false);

        if (consultation == null)
        {
            throw new NotFoundException("Consultation not found.");
        }

        if (consultation.CallerId != actorId && consultation.CalleeId != actorId)
        {
            throw new ForbiddenException("You are not a participant of this consultation.");
        }

        if (consultation.Status == ConsultationStatus.Completed)
        {
            return;
        }

        consultation.Status = ConsultationStatus.Completed;
        consultation.EndTime = DateTime.UtcNow;
        consultationRepo.Update(consultation);

        var bookingRepo = _unitOfWork.GetRepository<ConsultationBooking>();
        var booking = await bookingRepo.FirstOrDefaultAsync(
            predicate: b => b.ConsultationId == consultationId,
            asNoTracking: false);

        if (booking != null)
        {
            booking.Status = BookingStatus.Completed;
            bookingRepo.Update(booking);

            var slotRepo = _unitOfWork.GetRepository<ExpertTimeSlot>();
            var slot = await slotRepo.FirstOrDefaultAsync(
                predicate: s => s.Id == booking.TimeSlotId,
                asNoTracking: false);

            if (slot != null && slot.Status == TimeSlotStatus.Reserved)
            {
                slot.Status = TimeSlotStatus.Booked;
                slotRepo.Update(slot);
            }
        }

        await _unitOfWork.CommitAsync();
    }

    public async Task<UserFeedbackResponse> CreateConsultationReviewAsync(Guid consultationId, Guid raterId, CreateConsultationReviewRequest request)
    {
        var consultation = await _unitOfWork.GetRepository<Consultation>().FirstOrDefaultAsync(
            predicate: c => c.Id == consultationId,
            asNoTracking: false);

        if (consultation == null)
        {
            throw new NotFoundException("Consultation not found.");
        }

        if (consultation.CallerId != raterId)
        {
            throw new ForbiddenException("Only the user who booked the consultation can submit a review.");
        }

        if (consultation.Status != ConsultationStatus.Completed)
        {
            throw new BusinessException("Consultation must be completed before review submission.");
        }

        var existingReview = await _unitOfWork.GetRepository<UserFeedback>().FirstOrDefaultAsync(
            predicate: f => f.RaterId == raterId && f.ReferenceId == consultationId && f.Type == FeedbackType.Consultation);

        if (existingReview != null)
        {
            throw new ConflictException("You have already submitted a review for this consultation.");
        }

        var feedback = new UserFeedback
        {
            Id = Guid.NewGuid(),
            RaterId = raterId,
            TargetUserId = consultation.CalleeId,
            ReferenceId = consultationId,
            Type = FeedbackType.Consultation,
            Rating = request.Rating,
            Comments = request.Comments
        };

        await _unitOfWork.GetRepository<UserFeedback>().InsertAsync(feedback);

        var expertProfile = await _unitOfWork.GetRepository<ExpertProfile>().FirstOrDefaultAsync(
            predicate: p => p.AccountId == consultation.CalleeId,
            asNoTracking: false);

        if (expertProfile == null)
        {
            throw new NotFoundException("Expert profile not found.");
        }

        var newCount = expertProfile.RatingCount + 1;
        var newRating = ((expertProfile.Rating * expertProfile.RatingCount) + request.Rating) / newCount;
        expertProfile.RatingCount = newCount;
        expertProfile.Rating = newRating;
        _unitOfWork.GetRepository<ExpertProfile>().Update(expertProfile);

        await _unitOfWork.CommitAsync();

        var rater = await _unitOfWork.GetRepository<Account>().FirstOrDefaultAsync(predicate: a => a.Id == raterId);
        var expert = await _unitOfWork.GetRepository<Account>().FirstOrDefaultAsync(predicate: a => a.Id == consultation.CalleeId);

        _logger.LogInformation("Consultation review created. consultationId={ConsultationId}, raterId={RaterId}", consultationId, raterId);

        return new UserFeedbackResponse
        {
            Id = feedback.Id,
            RaterId = feedback.RaterId,
            TargetUserId = feedback.TargetUserId,
            ReferenceId = feedback.ReferenceId,
            Type = feedback.Type,
            Rating = feedback.Rating,
            Comments = feedback.Comments,
            CreatedAt = feedback.CreatedAt,
            UpdatedAt = feedback.UpdatedAt,
            RaterName = rater?.FullName,
            TargetUserName = expert?.FullName,
            UpdatedAverageRating = newRating,
            UpdatedRatingCount = newCount
        };
    }
}
