using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.FirstAidGuideline;
using SnakeAid.Core.Responses.FirstAidGuideline;

namespace SnakeAid.Service.Interfaces
{
    public interface IFirstAidGuidelineService
    {
        /// <summary>
        /// Create a new first aid guideline
        /// </summary>
        Task<ApiResponse<FirstAidGuidelineResponse>> CreateFirstAidGuidelineAsync(CreateFirstAidGuidelineRequest request);

        /// <summary>
        /// Get first aid guideline by ID
        /// </summary>
        Task<ApiResponse<FirstAidGuidelineResponse>> GetFirstAidGuidelineByIdAsync(int id);

        /// <summary>
        /// Get list of first aid guidelines with pagination and filters
        /// </summary>
        Task<ApiResponse<PagedData<FirstAidGuidelineResponse>>> GetFirstAidGuidelinesAsync(GetFirstAidGuidelineRequest request);

        /// <summary>
        /// Update an existing first aid guideline
        /// </summary>
        Task<ApiResponse<FirstAidGuidelineResponse>> UpdateFirstAidGuidelineAsync(int id, UpdateFirstAidGuidelineRequest request);

        /// <summary>
        /// Delete a first aid guideline
        /// </summary>
        Task<ApiResponse<bool>> DeleteFirstAidGuidelineAsync(int id);
    }
}
