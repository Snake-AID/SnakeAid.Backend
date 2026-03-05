using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.Expert;
using SnakeAid.Core.Responses.Expert;
using SnakeAid.Core.Responses.UserFeedback;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Service.Implements
{
    public class ExpertService : IExpertService
    {
        private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
        private readonly ILogger<ExpertService> _logger;

        public ExpertService(IUnitOfWork<SnakeAidDbContext> unitOfWork, ILogger<ExpertService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task UpdateSettingsAsync(Guid expertId, ExpertSettingsRequest request)
        {
            try
            {
                var profile = await _unitOfWork.GetRepository<ExpertProfile>().FirstOrDefaultAsync(
                    predicate: p => p.AccountId == expertId,
                    asNoTracking: false
                );

                if (profile == null) throw new NotFoundException("Expert profile not found.");

                profile.Biography = request.Biography;
                profile.ConsultationFee = request.ConsultationFee;

                _unitOfWork.GetRepository<ExpertProfile>().Update(profile);
                await _unitOfWork.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating expert settings.");
                throw;
            }
        }

        public async Task CreateBulkTimeSlotsAsync(Guid expertId, BulkTimeSlotRequest request)
        {
            var startDt = NormalizeWeekStartUtc(request.WeekStartDate);
            var endDt = startDt.AddDays(7);

            try
            {
                await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var repo = _unitOfWork.GetRepository<ExpertTimeSlot>();

                    var existingSlots = await repo.GetListAsync(
                        predicate: ts => ts.ExpertId == expertId && ts.StartTime >= startDt && ts.EndTime <= endDt
                    );

                    var slotsToAdd = new List<ExpertTimeSlot>();

                    foreach (var dayBlock in request.Days)
                    {
                        int dayOffset = (int)dayBlock.DayOfWeek - (int)startDt.DayOfWeek;
                        if (dayOffset < 0) dayOffset += 7;
                        var targetDate = startDt.AddDays(dayOffset);

                        foreach (var tb in dayBlock.TimeBlocks)
                        {
                            var start = DateTime.SpecifyKind(targetDate.Add(tb.StartTime), DateTimeKind.Utc);
                            var end = DateTime.SpecifyKind(targetDate.Add(tb.EndTime), DateTimeKind.Utc);

                            if (start >= end) continue;

                            var curr = start;
                            while (curr.AddMinutes(30) <= end)
                            {
                                var slotEnd = curr.AddMinutes(30);
                                var overlapsExisting = existingSlots.Any(s => s.StartTime < slotEnd && s.EndTime > curr);
                                var overlapsPending = slotsToAdd.Any(s => s.StartTime < slotEnd && s.EndTime > curr);

                                if (!overlapsExisting && !overlapsPending)
                                {
                                    slotsToAdd.Add(new ExpertTimeSlot
                                    {
                                        Id = Guid.NewGuid(),
                                        ExpertId = expertId,
                                        StartTime = curr,
                                        EndTime = slotEnd,
                                        Status = TimeSlotStatus.Available,
                                        Version = 0
                                    });
                                }
                                curr = slotEnd;
                            }
                        }
                    }

                    if (slotsToAdd.Any())
                    {
                        await repo.InsertRangeAsync(slotsToAdd);
                        await _unitOfWork.CommitAsync();
                    }
                });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Conflict when creating expert time slots for expertId={ExpertId}.", expertId);
                throw new ConflictException("Some time slots already exist. Please refresh and try again.");
            }
        }

        public async Task<PagingResponse<ExpertProfileResponse>> GetExpertsAsync(PaginationRequest request)
        {
            var pagedData = await _unitOfWork.GetRepository<ExpertProfile>().GetPagingListAsync(
                predicate: p => p.Account.IsActive,
                include: q => q.Include(p => p.Account).Include(p => p.Specializations).ThenInclude(es => es.Specialization),
                orderBy: q => q.OrderByDescending(p => p.Rating).ThenByDescending(p => p.RatingCount),
                page: request.PageNumber,
                size: request.PageSize
            );

            return new PagingResponse<ExpertProfileResponse>
            {
                Items = pagedData.Items.Select(p => new ExpertProfileResponse
                {
                    AccountId = p.AccountId,
                    Name = p.Account?.FullName ?? string.Empty,
                    AvatarUrl = p.Account?.AvatarUrl,
                    Biography = p.Biography,
                    IsOnline = p.IsOnline,
                    ConsultationFee = p.ConsultationFee,
                    Rating = p.Rating,
                    RatingCount = p.RatingCount,
                    IsVerified = true,
                    Specializations = p.Specializations.Select(s => s.Specialization.Name).ToList()
                }).ToList(),
                Meta = pagedData.Meta
            };
        }

        public async Task<ExpertProfileResponse> GetExpertProfileAsync(Guid expertId)
        {
            var p = await _unitOfWork.GetRepository<ExpertProfile>().FirstOrDefaultAsync(
                predicate: p => p.AccountId == expertId && p.Account.IsActive,
                include: q => q.Include(p => p.Account).Include(p => p.Specializations).ThenInclude(es => es.Specialization)
            );

            if (p == null) throw new NotFoundException("Expert profile not found.");

            return new ExpertProfileResponse
            {
                AccountId = p.AccountId,
                Name = p.Account?.FullName ?? string.Empty,
                AvatarUrl = p.Account?.AvatarUrl,
                Biography = p.Biography,
                IsOnline = p.IsOnline,
                ConsultationFee = p.ConsultationFee,
                Rating = p.Rating,
                RatingCount = p.RatingCount,
                IsVerified = true,
                Specializations = p.Specializations.Select(s => s.Specialization.Name).ToList()
            };
        }

        public async Task<PagingResponse<UserFeedbackResponse>> GetExpertReviewsAsync(Guid expertId, PaginationRequest request)
        {
            var pagedData = await _unitOfWork.GetRepository<UserFeedback>().GetPagingListAsync(
                predicate: f => f.TargetUserId == expertId && f.Type == FeedbackType.Consultation,
                include: q => q.Include(f => f.Rater).Include(f => f.TargetUser),
                orderBy: q => q.OrderByDescending(f => f.CreatedAt),
                page: request.PageNumber,
                size: request.PageSize
            );

            return new PagingResponse<UserFeedbackResponse>
            {
                Items = pagedData.Items.Select(feedback =>
                {
                    var res = feedback.Adapt<UserFeedbackResponse>();
                    res.RaterName = feedback.Rater?.FullName;
                    res.TargetUserName = feedback.TargetUser?.FullName;
                    return res;
                }).ToList(),
                Meta = pagedData.Meta
            };
        }

        public async Task<IEnumerable<ExpertTimeSlotResponse>> GetAvailableTimeSlotsAsync(Guid expertId)
        {
            var slots = await _unitOfWork.GetRepository<ExpertTimeSlot>().GetListAsync(
                predicate: ts => ts.ExpertId == expertId && ts.Status == TimeSlotStatus.Available && ts.StartTime > DateTime.UtcNow,
                orderBy: q => q.OrderBy(ts => ts.StartTime)
            );

            return slots.Adapt<List<ExpertTimeSlotResponse>>();
        }

        private static DateTime NormalizeWeekStartUtc(DateTime weekStartDate)
        {
            if (weekStartDate.Kind != DateTimeKind.Utc)
            {
                throw new ValidationException("WeekStartDate must be UTC. Use ISO 8601 format with 'Z'.");
            }

            return DateTime.SpecifyKind(weekStartDate.Date, DateTimeKind.Utc);
        }
    }
}
