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
                if (!request.ConsultationFee.HasValue)
                {
                    throw new ValidationException("ConsultationFee is required.");
                }

                var profile = await _unitOfWork.GetRepository<ExpertProfile>().FirstOrDefaultAsync(
                    predicate: p => p.AccountId == expertId,
                    asNoTracking: false
                );

                if (profile == null) throw new NotFoundException("Expert profile not found.");

                profile.Biography = request.Biography;
                profile.ConsultationFee = request.ConsultationFee.Value;

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
            ValidateBulkTimeSlotRequest(request);
            var startDt = NormalizeWeekStartUtc(request.WeekStartDate);
            var endDt = startDt.AddDays(7);

            try
            {
                await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var repo = _unitOfWork.GetRepository<ExpertTimeSlot>();

                    var existingSlots = await repo.GetListAsync(
                        predicate: ts => ts.ExpertId == expertId && ts.StartTime < endDt && ts.EndTime > startDt
                    );

                    var slotsToAdd = new List<ExpertTimeSlot>();

                    foreach (var dayBlock in request.Days)
                    {
                        int dayOffset = (int)dayBlock.DayOfWeek!.Value - (int)startDt.DayOfWeek;
                        if (dayOffset < 0) dayOffset += 7;
                        var targetDate = startDt.AddDays(dayOffset);

                        foreach (var tb in dayBlock.TimeBlocks)
                        {
                            var start = DateTime.SpecifyKind(targetDate.Add(tb.StartTime!.Value), DateTimeKind.Utc);
                            var end = DateTime.SpecifyKind(targetDate.Add(tb.EndTime!.Value), DateTimeKind.Utc);

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
                if (IsUniqueExpertTimeSlotViolation(ex))
                {
                    _logger.LogWarning(ex, "Unique slot conflict when creating expert time slots for expertId={ExpertId}.", expertId);
                    throw new ConflictException("Some time slots already exist. Please refresh and try again.");
                }

                _logger.LogError(ex, "Unexpected database update error when creating expert time slots for expertId={ExpertId}.", expertId);
                throw;
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

        private static DateTime NormalizeWeekStartUtc(DateTime? weekStartDate)
        {
            if (!weekStartDate.HasValue)
            {
                throw new ValidationException("WeekStartDate is required.");
            }

            var value = weekStartDate.Value;

            if (value.Kind != DateTimeKind.Utc)
            {
                throw new ValidationException("WeekStartDate must be UTC. Use ISO 8601 format with 'Z'.");
            }

            return DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);
        }

        private static void ValidateBulkTimeSlotRequest(BulkTimeSlotRequest request)
        {
            if (request.Days == null || request.Days.Count == 0)
            {
                throw new ValidationException("At least one day block is required.");
            }

            foreach (var dayBlock in request.Days)
            {
                if (!dayBlock.DayOfWeek.HasValue)
                {
                    throw new ValidationException("DayOfWeek is required.");
                }

                if (dayBlock.TimeBlocks == null || dayBlock.TimeBlocks.Count == 0)
                {
                    throw new ValidationException("At least one time block is required.");
                }

                foreach (var timeBlock in dayBlock.TimeBlocks)
                {
                    if (!timeBlock.StartTime.HasValue || !timeBlock.EndTime.HasValue)
                    {
                        throw new ValidationException("StartTime and EndTime are required.");
                    }

                    var start = timeBlock.StartTime.Value;
                    var end = timeBlock.EndTime.Value;
                    var startOfDay = TimeSpan.Zero;
                    var endOfDay = TimeSpan.FromHours(24);

                    if (start < startOfDay || start >= endOfDay)
                    {
                        throw new ValidationException("StartTime must be within [00:00:00, 24:00:00).");
                    }

                    if (end <= startOfDay || end > endOfDay)
                    {
                        throw new ValidationException("EndTime must be within (00:00:00, 24:00:00].");
                    }

                    if (end <= start)
                    {
                        throw new ValidationException("EndTime must be later than StartTime.");
                    }
                }
            }
        }

        private static bool IsUniqueExpertTimeSlotViolation(DbUpdateException ex)
        {
            const string indexName = "IX_ExpertTimeSlots_ExpertId_StartTime_EndTime";
            var message = $"{ex.Message} {ex.InnerException?.Message}";

            if (message.Contains(indexName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
                || message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase);
        }
    }
}
