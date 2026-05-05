using Mapster;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Constants;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Mappings;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.Consultation;
using SnakeAid.Core.Responses.Consultation;
using SnakeAid.Core.Responses.Consultation.History;
using SnakeAid.Core.Responses.UserFeedback;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Hubs;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Service.Implements;

public class ConsultationService : IConsultationService
{
    private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
    private readonly IConsultationPaymentService _consultationPaymentService;
    private readonly ILogger<ConsultationService> _logger;
    private readonly IHubContext<ConsultationHub>? _hubContext;
    private readonly ILiveKitService? _liveKitService;

#region Constructor
    static ConsultationService()
    {
        MapsterConfig.RegisterMappings();
    }

    public ConsultationService(
        IUnitOfWork<SnakeAidDbContext> unitOfWork,
        IConsultationPaymentService consultationPaymentService,
        ILogger<ConsultationService> logger,
        IHubContext<ConsultationHub>? hubContext = null,
        ILiveKitService? liveKitService = null)
    {
        _unitOfWork = unitOfWork;
        _consultationPaymentService = consultationPaymentService;
        _logger = logger;
        _hubContext = hubContext;
        _liveKitService = liveKitService;
    }
#endregion

#region Consultation Lifecycle
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

        var roomName = string.IsNullOrWhiteSpace(consultation.RoomId)
            ? $"consultation-{consultationId}"
            : consultation.RoomId;

        if (_hubContext != null)
        {
            try
            {
                await _hubContext.Clients.Group($"consultation:{consultationId}")
                    .SendAsync(ConsultationRealtimeEvents.ConsultationCallEnded, new
                    {
                        ConsultationId = consultationId,
                        Reason = ConsultationRealtimeEvents.ConsultationCallEndReasons.ParticipantEnded
                    });

                _logger.LogInformation(
                    "Sent ConsultationCallEnded signal for manually ended consultation {ConsultationId}, RoomId={RoomId}",
                    consultationId,
                    roomName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to send ConsultationCallEnded signal for manually ended consultation {ConsultationId}, RoomId={RoomId}",
                    consultationId,
                    roomName);
            }
        }

        if (_liveKitService != null)
        {
            try
            {
                await _liveKitService.DeleteRoomAsync(roomName);

                _logger.LogInformation(
                    "Deleted LiveKit room for manually ended consultation {ConsultationId}, RoomId={RoomId}",
                    consultationId,
                    roomName);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to delete LiveKit room for manually ended consultation {ConsultationId}, RoomId={RoomId}",
                    consultationId,
                    roomName);
            }
        }

        if (consultation.Status is ConsultationStatus.ExpertAbsent or ConsultationStatus.ExpertAbsentHandled)
        {
            consultation.EndTime ??= DateTime.UtcNow;
            consultationRepo.Update(consultation);
            await _unitOfWork.CommitAsync();
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

    public async Task<MyConsultationResponse> ReportExpertAbsentAsync(Guid consultationId, Guid memberId, ReportExpertAbsentRequest request)
    {
        var consultation = await _unitOfWork.GetRepository<Consultation>().FirstOrDefaultAsync(
            predicate: c => c.Id == consultationId,
            include: q => q.Include(c => c.Callee),
            asNoTracking: false);

        if (consultation == null)
        {
            throw new NotFoundException("Consultation not found.");
        }

        if (consultation.CallerId != memberId)
        {
            throw new ForbiddenException("Only the member of this consultation can report expert absence.");
        }

        if (consultation.Type != ConsultationType.Scheduled)
        {
            throw new BusinessException("Only scheduled consultations support expert absence reporting.");
        }

        if (consultation.StartTime > DateTime.UtcNow)
        {
            throw new BusinessException("Expert absence can only be reported after the consultation start time.");
        }

        if (!string.IsNullOrWhiteSpace(consultation.CustomerReport))
        {
            throw new ConflictException("Expert absence has already been reported for this consultation.");
        }

        if (consultation.Status is ConsultationStatus.Cancelled or ConsultationStatus.Completed or ConsultationStatus.AllAbsent or ConsultationStatus.UserAbsent)
        {
            throw new BusinessException($"Cannot report expert absence when consultation status is {consultation.Status}.");
        }

        if (request == null || request.CustomerReport == null)
        {
            throw new BusinessException("Customer report is required.");
        }

        if (request.CustomerReport.Length > 2000)
        {
            throw new BusinessException("Customer report must be at most 2000 characters.");
        }

        var normalizedReport = request.CustomerReport.Trim();
        if (string.IsNullOrWhiteSpace(normalizedReport))
        {
            throw new BusinessException("Customer report is required.");
        }

        consultation.CustomerReport = normalizedReport;
        consultation.CustomerReportSubmittedAt = DateTime.UtcNow;
        consultation.Status = ConsultationStatus.ExpertAbsent;

        _unitOfWork.GetRepository<Consultation>().Update(consultation);
        await _unitOfWork.CommitAsync();

        var booking = await _unitOfWork.GetRepository<ConsultationBooking>().FirstOrDefaultAsync(
            predicate: b => b.ConsultationId == consultationId,
            include: q => q.Include(b => b.TimeSlot));

        return BuildMyConsultationResponse(consultation, booking);
    }
#endregion

#region Consultation Feedback
    public async Task<PagingResponse<ConsultationMessageHistoryItemResponse>> GetConsultationMessageHistoryAsync(
        Guid consultationId,
        Guid actorId,
        bool isAdmin,
        ConsultationMessageHistoryQueryRequest query)
    {
        var consultation = await _unitOfWork.GetRepository<Consultation>().FirstOrDefaultAsync(
            predicate: c => c.Id == consultationId);

        if (consultation == null)
        {
            throw new NotFoundException("Consultation not found.");
        }

        if (!isAdmin && consultation.CallerId != actorId && consultation.CalleeId != actorId)
        {
            throw new ForbiddenException("You are not allowed to access this consultation message history.");
        }

        if (!IsTerminalMessageHistoryStatus(consultation.Status))
        {
            throw new BusinessException($"Consultation message history is available only for terminal consultations. Current status is {consultation.Status}.");
        }

        var messageRepo = _unitOfWork.GetRepository<ChatMessage>();
        var (normalizedPageNumber, normalizedPageSize) = NormalizePaging(pageNumber: query.PageNumber, pageSize: query.PageSize);

        var newestFirstQuery = messageRepo
            .CreateBaseQuery()
            .Where(m => m.ConsultationId == consultationId)
            .OrderByDescending(m => m.SentAt)
            .ThenByDescending(m => m.Id);

        var totalItems = await newestFirstQuery.CountAsync();

        var pageItems = await newestFirstQuery
            .Skip((normalizedPageNumber - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .Select(message => new ConsultationMessageHistoryItemResponse
            {
                Id = message.Id,
                ConsultationId = message.ConsultationId,
                SenderId = message.SenderId,
                Content = message.Content ?? string.Empty,
                AttachmentUrl = message.AttachmentUrl,
                SentAt = message.SentAt
            })
            .ToListAsync();

        return BuildNewestFirstPagingResponse(
            pageItems,
            totalItems,
            normalizedPageNumber,
            normalizedPageSize,
            item => item.SentAt,
            item => item.Id);
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
#endregion

#region Admin Consultation History
    public async Task<PagingResponse<AdminConsultationResponse>> GetAllConsultationsForAdminAsync(AdminConsultationsQueryRequest query)
    {
        var results = new List<AdminConsultationResponse>();
        var (includeScheduled, includeEmergency) = ResolveConsultationTypeInclusion(query.Type, nameof(query.Type));
        var statusFilter = ParseConsultationStatusFilter(query.Status);

        if (includeScheduled)
        {
            var bookings = await _unitOfWork.GetRepository<ConsultationBooking>().GetListAsync(
                predicate: b => b.ConsultationId.HasValue
                    && (!statusFilter.HasValue || b.Consultation!.Status == statusFilter.Value),
                include: q => q
                    .Include(b => b.User)
                    .Include(b => b.Expert)
                    .Include(b => b.TimeSlot)
                    .Include(b => b.Consultation));

            foreach (var booking in bookings)
            {
                if (booking.Consultation is null)
                {
                    continue;
                }

                results.Add(MapScheduledAdminConsultationResponse(booking.Consultation, booking));
            }

            var scheduledConsultationIds = results
                .Where(r => r.Type == ConsultationType.Scheduled.ToString())
                .Select(r => r.ConsultationId)
                .ToHashSet();

            var orphanedScheduled = await _unitOfWork.GetRepository<Consultation>().GetListAsync(
                predicate: c => c.Type == ConsultationType.Scheduled
                    && (!statusFilter.HasValue || c.Status == statusFilter.Value)
                    && !scheduledConsultationIds.Contains(c.Id),
                include: q => q
                    .Include(c => c.Caller)
                    .Include(c => c.Callee));

            foreach (var consultation in orphanedScheduled)
            {
                var hasBooking = await _unitOfWork.GetRepository<ConsultationBooking>().FirstOrDefaultAsync(
                    predicate: b => b.ConsultationId == consultation.Id);

                if (hasBooking != null)
                {
                    continue;
                }

                _logger.LogWarning(
                    "Scheduled consultation {ConsultationId} has no associated ConsultationBooking. Admin history price will be null.",
                    consultation.Id);

                results.Add(MapScheduledAdminConsultationResponse(consultation));
            }
        }

        if (includeEmergency)
        {
            var allEmergencyConsultationIds = await _unitOfWork.GetRepository<Consultation>().GetListAsync(
                predicate: c => c.Type == ConsultationType.Emergency
                    && (!statusFilter.HasValue || c.Status == statusFilter.Value));

            var emergencyRequests = await _unitOfWork.GetRepository<ConsultationPingRequest>().GetListAsync(
                predicate: p => p.ConsultationId.HasValue
                    && p.Status == ConsultationPingStatus.AcceptedByExpert
                    && (!statusFilter.HasValue || p.Consultation!.Status == statusFilter.Value),
                include: q => q
                    .Include(p => p.Rescuer)
                    .Include(p => p.Expert)
                    .Include(p => p.Consultation));

            var emergencyRequestIds = emergencyRequests.Select(p => p.Id).ToList();
            var emergencyConsultationIds = allEmergencyConsultationIds
                .Select(c => c.Id)
                .ToList();

            var consultationPayments = emergencyRequestIds.Count > 0
                ? await _unitOfWork.GetRepository<Transaction>().GetListAsync(
                    predicate: t => t.TransactionType == TransactionType.ConsultationPayment
                        && emergencyRequestIds.Contains(t.ReferenceId))
                : new List<Transaction>();
            var expertPayouts = emergencyConsultationIds.Count > 0
                ? await _unitOfWork.GetRepository<Transaction>().GetListAsync(
                    predicate: t => t.TransactionType == TransactionType.ExpertPayout
                        && emergencyConsultationIds.Contains(t.ReferenceId))
                : new List<Transaction>();

            var paymentLookup = consultationPayments
                .GroupBy(t => t.ReferenceId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(t => t.CreatedAt).First().Amount);
            var payoutLookup = expertPayouts
                .GroupBy(t => t.ReferenceId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(t => t.CreatedAt).First().Amount);

            foreach (var request in emergencyRequests)
            {
                if (request.Consultation is null)
                {
                    continue;
                }

                var consultation = request.Consultation;
                results.Add(MapEmergencyAdminConsultationResponse(
                    consultation,
                    ResolveEmergencyPrice(consultation.Id, request.Id, paymentLookup, payoutLookup),
                    request));
            }

            var mappedEmergencyConsultationIds = results
                .Where(r => r.Type == ConsultationType.Emergency.ToString())
                .Select(r => r.ConsultationId)
                .ToHashSet();

            var orphanedEmergency = await _unitOfWork.GetRepository<Consultation>().GetListAsync(
                predicate: c => c.Type == ConsultationType.Emergency
                    && (!statusFilter.HasValue || c.Status == statusFilter.Value)
                    && !mappedEmergencyConsultationIds.Contains(c.Id),
                include: q => q
                    .Include(c => c.Caller)
                    .Include(c => c.Callee));

            foreach (var consultation in orphanedEmergency)
            {
                var hasPingRequest = await _unitOfWork.GetRepository<ConsultationPingRequest>().FirstOrDefaultAsync(
                    predicate: p => p.ConsultationId == consultation.Id);

                if (hasPingRequest != null)
                {
                    continue;
                }

                _logger.LogWarning(
                    "Emergency consultation {ConsultationId} has no associated ConsultationPingRequest. Admin history emergency request id will be null.",
                    consultation.Id);

                results.Add(MapEmergencyAdminConsultationResponse(
                    consultation,
                    ResolveEmergencyPrice(consultation.Id, null, paymentLookup, payoutLookup)));
            }
        }

        return BuildPagingResponse(results, query.PageNumber, query.PageSize, c => c.StartTime);
    }

    public async Task<AdminConsultationResponse> GetConsultationByIdForAdminAsync(Guid consultationId)
    {
        var consultation = await _unitOfWork.GetRepository<Consultation>().FirstOrDefaultAsync(
            predicate: c => c.Id == consultationId,
            include: q => q
                .Include(c => c.Caller)
                .Include(c => c.Callee));

        if (consultation == null)
        {
            throw new NotFoundException("Consultation not found.");
        }

        return consultation.Type switch
        {
            ConsultationType.Scheduled => await BuildScheduledAdminConsultationDetailAsync(consultation),
            ConsultationType.Emergency => await BuildEmergencyAdminConsultationDetailAsync(consultation),
            _ => throw new ArgumentOutOfRangeException(nameof(consultation.Type), consultation.Type, "Unsupported consultation type.")
        };
    }

    public async Task<AdminConsultationResponse> ConfirmExpertAbsentHandledAsync(Guid consultationId)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var consultationRepo = _unitOfWork.GetRepository<Consultation>();
            var consultation = await consultationRepo.FirstOrDefaultAsync(
                predicate: c => c.Id == consultationId,
                asNoTracking: false);

            if (consultation == null)
            {
                throw new NotFoundException("Consultation not found.");
            }

            var bookingRepo = _unitOfWork.GetRepository<ConsultationBooking>();
            var booking = await bookingRepo.FirstOrDefaultAsync(
                predicate: b => b.ConsultationId == consultationId,
                asNoTracking: false);

            if (consultation.Status == ConsultationStatus.ExpertAbsentHandled
                && booking?.Status == BookingStatus.Refunded)
            {
                return;
            }

            if (consultation.Status is not (ConsultationStatus.ExpertAbsent or ConsultationStatus.ExpertAbsentHandled))
            {
                throw new BusinessException($"Only consultations in status {ConsultationStatus.ExpertAbsent} can be marked as handled.");
            }

            if (booking == null)
            {
                throw new NotFoundException("Consultation booking was not found.");
            }

            if (booking.Status != BookingStatus.Refunded)
            {
                await _consultationPaymentService.RefundScheduledBookingAsync(
                    booking.Id,
                    booking.UserId,
                    "Expert absent case approved by admin.",
                    CancellationToken.None);

                booking.Status = BookingStatus.Refunded;
                bookingRepo.Update(booking);
            }

            consultation.Status = ConsultationStatus.ExpertAbsentHandled;
            consultationRepo.Update(consultation);
            await _unitOfWork.CommitAsync();
        });

        return await GetConsultationByIdForAdminAsync(consultationId);
    }
#endregion

#region Expert Consultation History
    public async Task<PagingResponse<ExpertConsultationHistoryUnionResponse>> GetExpertConsultationsAsync(Guid expertId, MyConsultationsQueryRequest query)
    {
        var results = new List<ExpertConsultationHistoryUnionResponse>();

        var includeScheduled = string.IsNullOrEmpty(query.Type)
            || query.Type.Equals("Scheduled", StringComparison.OrdinalIgnoreCase);
        var includeEmergency = string.IsNullOrEmpty(query.Type)
            || query.Type.Equals("Emergency", StringComparison.OrdinalIgnoreCase);

        var statusFilter = ParseConsultationStatusFilter(query.Status);

        // Scheduled consultations — query by ExpertId
        if (includeScheduled)
        {
            var bookings = await _unitOfWork.GetRepository<ConsultationBooking>().GetListAsync(
                predicate: b => b.ExpertId == expertId
                    && b.ConsultationId.HasValue
                    && (!statusFilter.HasValue || b.Consultation!.Status == statusFilter.Value),
                include: q => q.Include(b => b.User).Include(b => b.TimeSlot).Include(b => b.Consultation));

            foreach (var b in bookings)
            {
                results.Add(new ExpertConsultationHistoryResponse
                {
                    ConsultationId = b.ConsultationId!.Value,
                    Type = "Scheduled",
                    Status = b.Consultation!.Status.ToString(),
                    UserId = b.UserId,
                    UserName = b.User?.FullName,
                    UserAvatarUrl = b.User?.AvatarUrl,
                    RoomId = b.Consultation.RoomId,
                    StartTime = b.Consultation.StartTime,
                    EndTime = b.Consultation.EndTime,
                    GrossPrice = b.Price,
                    NetPrice = null,
                    BookingId = b.Id,
                    SlotStartTime = b.TimeSlot?.StartTime,
                    SlotEndTime = b.TimeSlot?.EndTime
                });
            }
        }

        // Emergency consultations use Rescuer navigation for the requesting user account.
        if (includeEmergency)
        {
            var emergencyRequests = await _unitOfWork.GetRepository<ConsultationPingRequest>().GetListAsync(
                predicate: p => p.ExpertId == expertId
                              && p.ConsultationId.HasValue
                              && p.Status == ConsultationPingStatus.AcceptedByExpert
                              && (!statusFilter.HasValue || p.Consultation!.Status == statusFilter.Value),
                include: q => q.Include(p => p.Rescuer).Include(p => p.Consultation));

            var emergencyConsultationIds = emergencyRequests
                .Where(p => p.ConsultationId.HasValue)
                .Select(p => p.ConsultationId!.Value)
                .Distinct()
                .ToList();
            var emergencyRequestIds = emergencyRequests
                .Select(p => p.Id)
                .Distinct()
                .ToList();
            var payoutLookup = await BuildLatestTransactionAmountLookupAsync(
                emergencyConsultationIds,
                TransactionType.ExpertPayout);
            var paymentLookup = await BuildLatestTransactionAmountLookupAsync(
                emergencyRequestIds,
                TransactionType.ConsultationPayment);

            foreach (var request in emergencyRequests)
            {
                if (request.Consultation is null)
                    continue;

                var consultation = request.Consultation;
                var requester = request.Rescuer;
                results.Add(new ExpertConsultationHistoryResponse
                {
                    ConsultationId = consultation.Id,
                    Type = "Emergency",
                    Status = consultation.Status.ToString(),
                    UserId = request.RescuerId,
                    UserName = requester?.FullName,
                    UserAvatarUrl = requester?.AvatarUrl,
                    RoomId = consultation.RoomId,
                    StartTime = consultation.StartTime,
                    EndTime = consultation.EndTime,
                    GrossPrice = ResolveLookupAmount(paymentLookup, request.Id),
                    NetPrice = ResolveLookupAmount(payoutLookup, consultation.Id),
                    EmergencyRequestId = request.Id
                });
            }
        }

        // Handle edge case: Scheduled consultations that exist in Consultation table
        // but have no matching ConsultationBooking (Task 2.3)
        if (includeScheduled)
        {
            var scheduledConsultationIds = results
                .OfType<ExpertConsultationHistoryResponse>()
                .Where(r => r.Type == "Scheduled")
                .Select(r => r.ConsultationId)
                .ToHashSet();

            var orphanedScheduled = await _unitOfWork.GetRepository<Consultation>().GetListAsync(
                predicate: c => c.CalleeId == expertId
                    && c.Type == ConsultationType.Scheduled
                    && (!statusFilter.HasValue || c.Status == statusFilter.Value)
                    && !scheduledConsultationIds.Contains(c.Id),
                include: q => q.Include(c => c.Caller));

            // Check for consultations that have no booking at all
            foreach (var c in orphanedScheduled)
            {
                var hasBooking = await _unitOfWork.GetRepository<ConsultationBooking>().FirstOrDefaultAsync(
                    predicate: b => b.ConsultationId == c.Id);

                if (hasBooking == null)
                {
                    _logger.LogWarning(
                        "Scheduled consultation {ConsultationId} has no associated ConsultationBooking. GrossPrice will be null.",
                        c.Id);

                    results.Add(new ExpertConsultationHistoryResponse
                    {
                        ConsultationId = c.Id,
                        Type = "Scheduled",
                        Status = c.Status.ToString(),
                        UserId = c.CallerId,
                        UserName = c.Caller?.FullName,
                        UserAvatarUrl = c.Caller?.AvatarUrl,
                        RoomId = c.RoomId,
                        StartTime = c.StartTime,
                        EndTime = c.EndTime,
                        GrossPrice = null,
                        NetPrice = null
                    });
                }
            }
        }

        if (includeScheduled)
        {
            var scheduledPayoutLookup = await BuildLatestTransactionAmountLookupAsync(
                results
                    .OfType<ExpertConsultationHistoryResponse>()
                    .Where(r => r.Type == "Scheduled")
                    .Select(r => r.ConsultationId)
                    .Distinct()
                    .ToList(),
                TransactionType.ExpertPayout);

            foreach (var scheduledResult in results.OfType<ExpertConsultationHistoryResponse>().Where(r => r.Type == "Scheduled"))
            {
                scheduledResult.NetPrice = ResolveLookupAmount(scheduledPayoutLookup, scheduledResult.ConsultationId);
            }
        }

        if (includeEmergency && !statusFilter.HasValue)
        {
            var terminalRequests = await _unitOfWork.GetRepository<ConsultationPingRequest>().GetListAsync(
                predicate: p => p.ExpertId == expertId
                    && !p.ConsultationId.HasValue
                    && (p.Status == ConsultationPingStatus.DeclinedByExpert
                        || p.Status == ConsultationPingStatus.Expired),
                include: q => q.Include(p => p.Rescuer));

            foreach (var request in terminalRequests)
            {
                results.Add(new ExpertInstantConsultationRequestHistoryResponse
                {
                    InstantRequestId = request.Id,
                    Type = "Emergency",
                    RequestStatus = request.Status.ToString(),
                    RequestedAt = request.RequestedAt,
                    RespondedAt = request.RespondedAt,
                    UserId = request.RescuerId,
                    UserName = request.Rescuer?.FullName,
                    UserAvatarUrl = request.Rescuer?.AvatarUrl
                });
            }
        }

        // Sort + paginate
        var (normalizedPageNumber, normalizedPageSize) = NormalizePaging(query.PageNumber, query.PageSize);
        return BuildPagingResponse(results, normalizedPageNumber, normalizedPageSize, c => c.HistorySortTime);
    }
#endregion

#region User Consultation History
    public async Task<PagingResponse<MyConsultationHistoryUnionResponse>> GetMyConsultationsAsync(Guid userId, MyConsultationsQueryRequest query)
    {
        var results = new List<MyConsultationHistoryUnionResponse>();

        var includeScheduled = string.IsNullOrEmpty(query.Type)
            || query.Type.Equals("Scheduled", StringComparison.OrdinalIgnoreCase);
        var includeEmergency = string.IsNullOrEmpty(query.Type)
            || query.Type.Equals("Emergency", StringComparison.OrdinalIgnoreCase);

        // Parse status filter once for DB-level filtering
        var statusFilter = ParseConsultationStatusFilter(query.Status);

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
                results.Add(BuildMyConsultationHistoryResponse(b.Consultation!, b));
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
                results.Add(new MyConsultationHistoryResponse
                {
                    ConsultationId = consultation.Id,
                    Type = "Emergency",
                    Status = consultation.Status.ToString(),
                    ExpertId = p.ExpertId,
                    ExpertName = p.Expert?.FullName,
                    ExpertAvatarUrl = p.Expert?.AvatarUrl,
                    RoomId = consultation.RoomId,
                    StartTime = consultation.StartTime,
                    EndTime = consultation.EndTime,
                    EmergencyRequestId = p.Id,
                    Price = transactionLookup.TryGetValue(p.Id, out var amount) ? amount : null,
                    CustomerReport = consultation.CustomerReport,
                    CustomerReportSubmittedAt = consultation.CustomerReportSubmittedAt
                });
            }
        }

        if (includeEmergency && !statusFilter.HasValue)
        {
            var terminalRequests = await _unitOfWork.GetRepository<ConsultationPingRequest>().GetListAsync(
                predicate: p => p.RescuerId == userId
                    && !p.ConsultationId.HasValue
                    && (p.Status == ConsultationPingStatus.DeclinedByExpert
                        || p.Status == ConsultationPingStatus.Expired),
                include: q => q.Include(p => p.Expert));

            foreach (var request in terminalRequests)
            {
                results.Add(new MyInstantConsultationRequestHistoryResponse
                {
                    InstantRequestId = request.Id,
                    Type = "Emergency",
                    RequestStatus = request.Status.ToString(),
                    RequestedAt = request.RequestedAt,
                    RespondedAt = request.RespondedAt,
                    ExpertId = request.ExpertId,
                    ExpertName = request.Expert?.FullName,
                    ExpertAvatarUrl = request.Expert?.AvatarUrl
                });
            }
        }

        // Sort + paginate
        var (normalizedPageNumber, normalizedPageSize) = NormalizePaging(query.PageNumber, query.PageSize);
        return BuildPagingResponse(results, normalizedPageNumber, normalizedPageSize, c => c.HistorySortTime);
    }
#endregion

#region Shared Parsing Helpers
    private static MyConsultationHistoryResponse BuildMyConsultationHistoryResponse(
        Consultation consultation,
        ConsultationBooking? booking = null)
    {
        return new MyConsultationHistoryResponse
        {
            ConsultationId = consultation.Id,
            Type = consultation.Type.ToString(),
            Status = consultation.Status.ToString(),
            ExpertId = consultation.CalleeId,
            ExpertName = consultation.Callee?.FullName ?? booking?.Expert?.FullName,
            ExpertAvatarUrl = consultation.Callee?.AvatarUrl ?? booking?.Expert?.AvatarUrl,
            RoomId = consultation.RoomId,
            StartTime = consultation.StartTime,
            EndTime = consultation.EndTime,
            Price = booking?.Price,
            ProblemDescription = booking?.ProblemDescription,
            CustomerReport = consultation.CustomerReport,
            CustomerReportSubmittedAt = consultation.CustomerReportSubmittedAt,
            BookingId = booking?.Id,
            SlotStartTime = booking?.TimeSlot?.StartTime,
            SlotEndTime = booking?.TimeSlot?.EndTime
        };
    }

    private static MyConsultationResponse BuildMyConsultationResponse(
        Consultation consultation,
        ConsultationBooking? booking = null)
    {
        return new MyConsultationResponse
        {
            ConsultationId = consultation.Id,
            Type = consultation.Type.ToString(),
            Status = consultation.Status.ToString(),
            ExpertId = consultation.CalleeId,
            ExpertName = consultation.Callee?.FullName ?? booking?.Expert?.FullName,
            ExpertAvatarUrl = consultation.Callee?.AvatarUrl ?? booking?.Expert?.AvatarUrl,
            RoomId = consultation.RoomId,
            StartTime = consultation.StartTime,
            EndTime = consultation.EndTime,
            Price = booking?.Price,
            ProblemDescription = booking?.ProblemDescription,
            CustomerReport = consultation.CustomerReport,
            CustomerReportSubmittedAt = consultation.CustomerReportSubmittedAt,
            BookingId = booking?.Id,
            SlotStartTime = booking?.TimeSlot?.StartTime,
            SlotEndTime = booking?.TimeSlot?.EndTime
        };
    }

    private static ConsultationStatus? ParseConsultationStatusFilter(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return null;
        }

        if (!Enum.TryParse<ConsultationStatus>(status, ignoreCase: true, out var parsed))
        {
            throw new ArgumentException($"Invalid status value: {status}", nameof(status));
        }

        return parsed;
    }

    private static (bool includeScheduled, bool includeEmergency) ResolveConsultationTypeInclusion(string? type, string paramName)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            return (true, true);
        }

        if (!Enum.TryParse<ConsultationType>(type, ignoreCase: true, out var parsed))
        {
            throw new ArgumentException($"Invalid type value: {type}", paramName);
        }

        return parsed switch
        {
            ConsultationType.Scheduled => (true, false),
            ConsultationType.Emergency => (false, true),
            _ => throw new ArgumentOutOfRangeException(paramName, type, "Unsupported consultation type.")
        };
    }

    private static bool IsTerminalMessageHistoryStatus(ConsultationStatus status)
    {
        return status is ConsultationStatus.Cancelled
            or ConsultationStatus.Completed
            or ConsultationStatus.UserAbsent
            or ConsultationStatus.ExpertAbsent
            or ConsultationStatus.ExpertAbsentHandled
            or ConsultationStatus.AllAbsent;
    }

    private static (int pageNumber, int pageSize) NormalizePaging(int pageNumber, int pageSize)
    {
        var normalizedPageNumber = pageNumber < 1 ? 1 : pageNumber;
        var normalizedPageSize = pageSize < 1 ? 10 : pageSize;
        return (normalizedPageNumber, normalizedPageSize);
    }

    private static PagingResponse<TItem> BuildNewestFirstPagingResponse<TItem, TOrderKey>(
        IEnumerable<TItem> newestFirstPageItems,
        int totalItems,
        int pageNumber,
        int pageSize,
        Func<TItem, DateTime> ascendingOrderSelector,
        Func<TItem, TOrderKey> tieBreakerSelector)
    {
        var (normalizedPageNumber, normalizedPageSize) = NormalizePaging(pageNumber, pageSize);

        var pageItems = newestFirstPageItems
            .OrderBy(ascendingOrderSelector)
            .ThenBy(tieBreakerSelector)
            .ToList();

        return new PagingResponse<TItem>
        {
            Items = pageItems,
            Meta = new PaginationMeta
            {
                CurrentPage = normalizedPageNumber,
                PageSize = normalizedPageSize,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)normalizedPageSize)
            }
        };
    }
#endregion

#region Admin Consultation Mapping Helpers
    private async Task<AdminConsultationResponse> BuildScheduledAdminConsultationDetailAsync(Consultation consultation)
    {
        var booking = await _unitOfWork.GetRepository<ConsultationBooking>().FirstOrDefaultAsync(
            predicate: b => b.ConsultationId == consultation.Id,
            include: q => q
                .Include(b => b.User)
                .Include(b => b.Expert)
                .Include(b => b.TimeSlot));

        if (booking == null)
        {
            _logger.LogWarning(
                "Scheduled consultation {ConsultationId} has no associated ConsultationBooking. Admin detail booking fields will be null.",
                consultation.Id);

            return MapScheduledAdminConsultationResponse(consultation);
        }

        return MapScheduledAdminConsultationResponse(consultation, booking);
    }

    private async Task<AdminConsultationResponse> BuildEmergencyAdminConsultationDetailAsync(Consultation consultation)
    {
        var request = await _unitOfWork.GetRepository<ConsultationPingRequest>().FirstOrDefaultAsync(
            predicate: p => p.ConsultationId == consultation.Id,
            include: q => q
                .Include(p => p.Rescuer)
                .Include(p => p.Expert));

        var price = await ResolveEmergencyPriceAsync(consultation.Id, request?.Id);

        if (request == null)
        {
            _logger.LogWarning(
                "Emergency consultation {ConsultationId} has no associated ConsultationPingRequest. Admin detail emergency request fields will be null.",
                consultation.Id);

            return MapEmergencyAdminConsultationResponse(consultation, price);
        }

        return MapEmergencyAdminConsultationResponse(consultation, price, request);
    }

    private static AdminConsultationResponse MapScheduledAdminConsultationResponse(
        Consultation consultation,
        ConsultationBooking? booking = null)
    {
        var response = consultation.Adapt<AdminConsultationResponse>();

        if (booking == null)
        {
            return response;
        }

        booking.Adapt(response);
        response.Type = consultation.Type.ToString();
        response.Status = consultation.Status.ToString();
        return response;
    }

    private static AdminConsultationResponse MapEmergencyAdminConsultationResponse(
        Consultation consultation,
        decimal? price,
        ConsultationPingRequest? request = null)
    {
        var response = consultation.Adapt<AdminConsultationResponse>();

        response.Price = price;

        if (request == null)
        {
            return response;
        }

        request.Adapt(response);
        response.Type = consultation.Type.ToString();
        response.Status = consultation.Status.ToString();
        return response;
    }

    private async Task<decimal?> ResolveEmergencyPriceAsync(Guid consultationId, Guid? requestId)
    {
        if (requestId.HasValue)
        {
            var consultationPayment = await _unitOfWork.GetRepository<Transaction>().FirstOrDefaultAsync(
                predicate: t => t.TransactionType == TransactionType.ConsultationPayment
                    && t.ReferenceId == requestId.Value,
                orderBy: q => q.OrderByDescending(t => t.CreatedAt));

            if (consultationPayment != null)
            {
                return consultationPayment.Amount;
            }
        }

        var expertPayout = await _unitOfWork.GetRepository<Transaction>().FirstOrDefaultAsync(
            predicate: t => t.TransactionType == TransactionType.ExpertPayout
                && t.ReferenceId == consultationId,
            orderBy: q => q.OrderByDescending(t => t.CreatedAt));

        return expertPayout?.Amount;
    }

    private static decimal? ResolveEmergencyPrice(
        Guid consultationId,
        Guid? requestId,
        IReadOnlyDictionary<Guid, decimal> paymentLookup,
        IReadOnlyDictionary<Guid, decimal> payoutLookup)
    {
        if (requestId.HasValue && paymentLookup.TryGetValue(requestId.Value, out var paymentAmount))
        {
            return paymentAmount;
        }

        return payoutLookup.TryGetValue(consultationId, out var payoutAmount)
            ? payoutAmount
            : null;
    }

    private async Task<IReadOnlyDictionary<Guid, decimal>> BuildLatestTransactionAmountLookupAsync(
        IReadOnlyCollection<Guid> referenceIds,
        TransactionType transactionType)
    {
        if (referenceIds.Count == 0)
        {
            return new Dictionary<Guid, decimal>();
        }

        var transactions = await _unitOfWork.GetRepository<Transaction>().GetListAsync(
            predicate: t => t.TransactionType == transactionType
                         && referenceIds.Contains(t.ReferenceId));

        return transactions
            .GroupBy(t => t.ReferenceId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(t => t.CreatedAt).First().Amount);
    }

    private static decimal? ResolveLookupAmount(IReadOnlyDictionary<Guid, decimal> lookup, Guid referenceId)
    {
        return lookup.TryGetValue(referenceId, out var amount)
            ? amount
            : null;
    }
#endregion

#region Generic Paging Helper
    private static PagingResponse<T> BuildPagingResponse<T>(
        IEnumerable<T> items,
        int pageNumber,
        int pageSize,
        Func<T, DateTime?> startTimeSelector)
    {
        var sorted = items
            .OrderByDescending(item => startTimeSelector(item) ?? DateTime.MinValue)
            .ToList();
        var totalItems = sorted.Count;
        var paged = sorted
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagingResponse<T>
        {
            Items = paged,
            Meta = new PaginationMeta
            {
                CurrentPage = pageNumber,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
            }
        };
    }
#endregion
}
