using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.Consultation;
using SnakeAid.Core.Responses.Consultation;
using SnakeAid.Core.Responses.UserFeedback;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Service.Implements;

public class ConsultationService : IConsultationService
{
    private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
    private readonly IConsultationPaymentService _consultationPaymentService;
    private readonly ILogger<ConsultationService> _logger;

    public ConsultationService(
        IUnitOfWork<SnakeAidDbContext> unitOfWork,
        IConsultationPaymentService consultationPaymentService,
        ILogger<ConsultationService> logger)
    {
        _unitOfWork = unitOfWork;
        _consultationPaymentService = consultationPaymentService;
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
        await _consultationPaymentService.SettleConsultationEscrowAsync(consultationId);
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

    public async Task<UserFeedbackResponse?> GetConsultationReviewAsync(Guid consultationId, Guid actorId)
    {
        var consultation = await _unitOfWork.GetRepository<Consultation>().FirstOrDefaultAsync(
            predicate: c => c.Id == consultationId);

        if (consultation == null)
            throw new NotFoundException("Consultation not found.");

        if (consultation.CallerId != actorId && consultation.CalleeId != actorId)
            throw new ForbiddenException("You are not a participant of this consultation.");

        var feedback = await _unitOfWork.GetRepository<UserFeedback>().FirstOrDefaultAsync(
            predicate: f => f.ReferenceId == consultationId && f.Type == FeedbackType.Consultation);

        if (feedback == null)
            return null;

        var rater = await _unitOfWork.GetRepository<Account>().FirstOrDefaultAsync(predicate: a => a.Id == feedback.RaterId);
        var target = await _unitOfWork.GetRepository<Account>().FirstOrDefaultAsync(predicate: a => a.Id == feedback.TargetUserId);

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
            TargetUserName = target?.FullName,
            UpdatedAverageRating = 0,
            UpdatedRatingCount = 0
        };
    }

    public async Task<PagingResponse<MyConsultationResponse>> GetMyConsultationsAsync(Guid userId, MyConsultationsQueryRequest query)
    {
        var results = new List<MyConsultationResponse>();

        var includeScheduled = string.IsNullOrEmpty(query.Type)
            || query.Type.Equals("Scheduled", StringComparison.OrdinalIgnoreCase);
        var includeEmergency = string.IsNullOrEmpty(query.Type)
            || query.Type.Equals("Emergency", StringComparison.OrdinalIgnoreCase);

        // Parse status filter once for DB-level filtering
        ConsultationStatus? statusFilter = null;
        if (!string.IsNullOrEmpty(query.Status)
            && Enum.TryParse<ConsultationStatus>(query.Status, ignoreCase: true, out var parsed))
        {
            statusFilter = parsed;
        }

        // Scheduled consultations
        if (includeScheduled)
        {
            var bookings = await _unitOfWork.GetRepository<ConsultationBooking>().GetListAsync(
                predicate: b => b.UserId == userId
                    && b.ConsultationId.HasValue
                    && (!statusFilter.HasValue || b.Consultation!.Status == statusFilter.Value),
                include: q => q.Include(b => b.Expert).Include(b => b.TimeSlot).Include(b => b.Consultation));

            foreach (var b in bookings)
            {
                results.Add(new MyConsultationResponse
                {
                    ConsultationId = b.ConsultationId!.Value,
                    Type = "Scheduled",
                    Status = b.Consultation!.Status.ToString(),
                    ExpertId = b.ExpertId,
                    ExpertName = b.Expert?.FullName,
                    RoomId = b.Consultation.RoomId,
                    StartTime = b.Consultation.StartTime,
                    EndTime = b.Consultation.EndTime,
                    Price = b.Price,
                    ProblemDescription = b.ProblemDescription,
                    BookingId = b.Id,
                    SlotStartTime = b.TimeSlot?.StartTime,
                    SlotEndTime = b.TimeSlot?.EndTime
                });
            }
        }

        // Emergency consultations (include Consultation directly — no separate query needed)
        if (includeEmergency)
        {
            var emergencyRequests = await _unitOfWork.GetRepository<ConsultationPingRequest>().GetListAsync(
                predicate: p => p.RescuerId == userId
                             && p.ConsultationId.HasValue
                             && p.Status == ConsultationPingStatus.AcceptedByExpert
                             && (!statusFilter.HasValue || p.Consultation!.Status == statusFilter.Value),
                include: q => q.Include(p => p.Expert).Include(p => p.Consultation));

            // Batch-fetch transactions for emergency consultations (single query, no N+1)
            var emergencyRequestIds = emergencyRequests.Select(p => p.Id).ToList();
            var emergencyTransactions = emergencyRequestIds.Count > 0
                ? await _unitOfWork.GetRepository<Transaction>().GetListAsync(
                    predicate: t => t.TransactionType == TransactionType.ConsultationPayment
                                 && emergencyRequestIds.Contains(t.ReferenceId))
                : new List<Transaction>();
            var transactionLookup = emergencyTransactions
                .GroupBy(t => t.ReferenceId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(t => t.CreatedAt).First().Amount);

            foreach (var p in emergencyRequests)
            {
                if (p.Consultation is null)
                    continue;

                var consultation = p.Consultation;
                results.Add(new MyConsultationResponse
                {
                    ConsultationId = consultation.Id,
                    Type = "Emergency",
                    Status = consultation.Status.ToString(),
                    ExpertId = p.ExpertId,
                    ExpertName = p.Expert?.FullName,
                    RoomId = consultation.RoomId,
                    StartTime = consultation.StartTime,
                    EndTime = consultation.EndTime,
                    EmergencyRequestId = p.Id,
                    Price = transactionLookup.TryGetValue(p.Id, out var amount) ? amount : null
                });
            }
        }

        // Sort + paginate
        var sorted = results.OrderByDescending(c => c.StartTime ?? DateTime.MinValue).ToList();
        var totalItems = sorted.Count;
        var paged = sorted.Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize);

        return new PagingResponse<MyConsultationResponse>
        {
            Items = paged,
            Meta = new PaginationMeta
            {
                CurrentPage = query.PageNumber,
                PageSize = query.PageSize,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)query.PageSize)
            }
        };
    }
}
