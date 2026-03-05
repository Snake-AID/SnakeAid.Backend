using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.Expert;
using SnakeAid.Core.Responses.Expert;
using SnakeAid.Core.Responses.UserFeedback;

namespace SnakeAid.Service.Interfaces
{
    public interface IExpertService
    {
        Task UpdateSettingsAsync(Guid expertId, ExpertSettingsRequest request);
        Task CreateBulkTimeSlotsAsync(Guid expertId, BulkTimeSlotRequest request);
        Task<PagingResponse<ExpertProfileResponse>> GetExpertsAsync(PaginationRequest request);
        Task<ExpertProfileResponse> GetExpertProfileAsync(Guid expertId);
        Task<PagingResponse<UserFeedbackResponse>> GetExpertReviewsAsync(Guid expertId, PaginationRequest request);
        Task<IEnumerable<ExpertTimeSlotResponse>> GetAvailableTimeSlotsAsync(Guid expertId);
    }
}
