using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Requests.UserFeedback;
using SnakeAid.Core.Responses.UserFeedback;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;
using System;
using System.Threading.Tasks;

namespace SnakeAid.Service.Implements
{
    public class UserFeedbackService : IUserFeedbackService
    {
        private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
        private readonly ILogger<UserFeedbackService> _logger;

        public UserFeedbackService(
            IUnitOfWork<SnakeAidDbContext> unitOfWork,
            ILogger<UserFeedbackService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<UserFeedbackResponse> CreateFeedbackAsync(CreateUserFeedbackRequest request, Guid raterId)
        {
            try
            {
                if (request == null)
                {
                    throw new BadRequestException("Request data cannot be null.");
                }

                if (raterId == request.TargetUserId)
                {
                    throw new BadRequestException("You cannot rate yourself.");
                }

                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    // Validate rater exists
                    var rater = await _unitOfWork.GetRepository<Account>().FirstOrDefaultAsync(
                        predicate: a => a.Id == raterId,
                        include: query => query.Include(a => a.MemberProfile)
                    );

                    if (rater == null)
                    {
                        throw new NotFoundException("Rater account not found.");
                    }

                    // Validate target user exists and load appropriate profile
                    var targetUser = await _unitOfWork.GetRepository<Account>().FirstOrDefaultAsync(
                        predicate: a => a.Id == request.TargetUserId,
                        include: query => query
                            .Include(a => a.MemberProfile)
                            .Include(a => a.ExpertProfile)
                            .Include(a => a.RescuerProfile)
                    );

                    if (targetUser == null)
                    {
                        throw new NotFoundException("Target user not found.");
                    }

                    // Validate reference based on type
                    await ValidateReferenceAsync(request.ReferenceId, request.Type);

                    // Check if feedback already exists for this reference
                    var existingFeedback = await _unitOfWork.GetRepository<Core.Domains.UserFeedback>().FirstOrDefaultAsync(
                        predicate: f => f.RaterId == raterId && f.ReferenceId == request.ReferenceId
                    );

                    if (existingFeedback != null)
                    {
                        throw new BadRequestException("You have already submitted feedback for this request.");
                    }

                    // Create new feedback
                    var newFeedback = new Core.Domains.UserFeedback
                    {
                        Id = Guid.NewGuid(),
                        RaterId = raterId,
                        TargetUserId = request.TargetUserId,
                        ReferenceId = request.ReferenceId,
                        Type = request.Type,
                        Rating = request.Rating,
                        Comments = request.Comments,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _unitOfWork.GetRepository<Core.Domains.UserFeedback>().InsertAsync(newFeedback);

                    // Update target user's rating based on role
                    var (updatedRating, updatedCount) = await UpdateUserRatingAsync(
                        request.TargetUserId,
                        request.TargetUserRole,
                        request.Rating
                    );

                    await _unitOfWork.CommitAsync();

                    // Prepare response
                    var response = newFeedback.Adapt<UserFeedbackResponse>();
                    response.RaterName = rater.FullName;
                    response.TargetUserName = targetUser.FullName;
                    response.UpdatedAverageRating = updatedRating;
                    response.UpdatedRatingCount = updatedCount;

                    return response;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user feedback: {Message}", ex.Message);
                throw;
            }
        }

        private async Task ValidateReferenceAsync(Guid referenceId, FeedbackType type)
        {
            switch (type)
            {
                case FeedbackType.Emergency:
                    var incident = await _unitOfWork.GetRepository<SnakebiteIncident>().FirstOrDefaultAsync(
                        predicate: i => i.Id == referenceId
                    );
                    if (incident == null)
                    {
                        throw new NotFoundException("Snakebite incident not found.");
                    }
                    break;

                case FeedbackType.Catching:
                    var catchingRequest = await _unitOfWork.GetRepository<SnakeCatchingRequest>().FirstOrDefaultAsync(
                        predicate: r => r.Id == referenceId
                    );
                    if (catchingRequest == null)
                    {
                        throw new NotFoundException("Snake catching request not found.");
                    }
                    break;

                case FeedbackType.Consultation:
                    // TODO: Add validation for expert consultation once it's implemented
                    _logger.LogWarning("Consultation feedback validation not yet implemented.");
                    break;

                default:
                    throw new BadRequestException("Invalid feedback type.");
            }
        }

        private async Task<(decimal averageRating, int ratingCount)> UpdateUserRatingAsync(
            Guid userId,
            AccountRole role,
            int newRating)
        {
            switch (role)
            {
                case AccountRole.User:
                    var memberProfile = await _unitOfWork.GetRepository<MemberProfile>().FirstOrDefaultAsync(
                        predicate: p => p.AccountId == userId,
                        asNoTracking: false
                    );

                    if (memberProfile == null)
                    {
                        throw new NotFoundException("Member profile not found.");
                    }

                    var memberNewCount = memberProfile.RatingCount + 1;
                    var memberNewTotal = (memberProfile.Rating * memberProfile.RatingCount) + newRating;
                    var memberNewAverage = memberNewTotal / memberNewCount;

                    memberProfile.Rating = memberNewAverage;
                    memberProfile.RatingCount = memberNewCount;
                    memberProfile.UpdatedAt = DateTime.UtcNow;

                    _unitOfWork.GetRepository<MemberProfile>().Update(memberProfile);

                    return ((decimal)memberNewAverage, memberNewCount);

                case AccountRole.Expert:
                    var expertProfile = await _unitOfWork.GetRepository<ExpertProfile>().FirstOrDefaultAsync(
                        predicate: p => p.AccountId == userId,
                        asNoTracking: false
                    );

                    if (expertProfile == null)
                    {
                        throw new NotFoundException("Expert profile not found.");
                    }

                    var expertNewCount = expertProfile.RatingCount + 1;
                    var expertNewTotal = (expertProfile.Rating * expertProfile.RatingCount) + newRating;
                    var expertNewAverage = expertNewTotal / expertNewCount;

                    expertProfile.Rating = expertNewAverage;
                    expertProfile.RatingCount = expertNewCount;
                    expertProfile.UpdatedAt = DateTime.UtcNow;

                    _unitOfWork.GetRepository<ExpertProfile>().Update(expertProfile);

                    return (expertNewAverage, expertNewCount);

                case AccountRole.Rescuer:
                    var rescuerProfile = await _unitOfWork.GetRepository<RescuerProfile>().FirstOrDefaultAsync(
                        predicate: p => p.AccountId == userId,
                        asNoTracking: false
                    );

                    if (rescuerProfile == null)
                    {
                        throw new NotFoundException("Rescuer profile not found.");
                    }

                    var rescuerNewCount = rescuerProfile.RatingCount + 1;
                    var rescuerNewTotal = (rescuerProfile.Rating * rescuerProfile.RatingCount) + newRating;
                    var rescuerNewAverage = rescuerNewTotal / rescuerNewCount;

                    rescuerProfile.Rating = rescuerNewAverage;
                    rescuerProfile.RatingCount = rescuerNewCount;
                    rescuerProfile.UpdatedAt = DateTime.UtcNow;

                    _unitOfWork.GetRepository<RescuerProfile>().Update(rescuerProfile);

                    return (rescuerNewAverage, rescuerNewCount);

                default:
                    throw new BadRequestException("Invalid user role for feedback.");
            }
        }
    }
}
