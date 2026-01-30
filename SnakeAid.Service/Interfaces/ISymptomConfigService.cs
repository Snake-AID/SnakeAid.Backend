using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.SymptomConfig;
using SnakeAid.Core.Responses.SymptomConfig;

namespace SnakeAid.Service.Interfaces
{
    public interface ISymptomConfigService
    {
        /// <summary>
        /// Create a new symptom configuration
        /// </summary>
        Task<ApiResponse<SymptomConfigResponse>> CreateSymptomConfigAsync(CreateSymptomConfigRequest request);

        /// <summary>
        /// Get symptom configuration by ID
        /// </summary>
        Task<ApiResponse<SymptomConfigResponse>> GetSymptomConfigByIdAsync(int id);

        /// <summary>
        /// Get list of symptom configurations with pagination and filters
        /// </summary>
        Task<ApiResponse<PagedData<SymptomConfigResponse>>> GetSymptomConfigsAsync(GetSymptomConfigRequest request);

        /// <summary>
        /// Update an existing symptom configuration
        /// </summary>
        Task<ApiResponse<SymptomConfigResponse>> UpdateSymptomConfigAsync(int id, UpdateSymptomConfigRequest request);

        /// <summary>
        /// Delete a symptom configuration
        /// </summary>
        Task<ApiResponse<bool>> DeleteSymptomConfigAsync(int id);

        /// <summary>
        /// Get symptom configurations grouped by AttributeKey
        /// </summary>
        Task<ApiResponse<Dictionary<string, List<SymptomConfigResponse>>>> GetSymptomConfigsGroupedByKeyAsync();
    }
}
