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
        Task<FirstAidGuidelineResponse> CreateFirstAidGuidelineAsync(CreateFirstAidGuidelineRequest request);

        /// <summary>
        /// Get first aid guideline by ID
        /// </summary>
        Task<FirstAidGuidelineResponse> GetFirstAidGuidelineByIdAsync(int id);

        /// <summary>
        /// Get list of first aid guidelines with pagination and filters
        /// </summary>
        Task<PagedData<FirstAidGuidelineResponse>> FilterFirstAidGuidelinesAsync(GetFirstAidGuidelineRequest request);

        /// <summary>
        /// Update an existing first aid guideline
        /// </summary>
        Task<FirstAidGuidelineResponse> UpdateFirstAidGuidelineAsync(int id, UpdateFirstAidGuidelineRequest request);

        /// <summary>
        /// Delete a first aid guideline
        /// </summary>
        Task DeleteFirstAidGuidelineAsync(int id);

        /// <summary>
        /// Get all first aid guidelines without pagination
        /// </summary>
        Task<List<FirstAidGuidelineResponse>> GetAllFirstAidGuidelineAsync();

        /// <summary>
        /// Get first aid guidelines by snake species ID
        /// </summary>
        Task<List<FirstAidGuidelineResponse>> GetFirstAidGuidelinesBySnakeSpeciesIdAsync(int snakeSpeciesId);
    }
}
