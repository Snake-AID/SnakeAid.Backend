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
        Task<SymptomConfigResponse> CreateSymptomConfigAsync(CreateSymptomConfigRequest request);

        /// <summary>
        /// Get symptom configuration by ID
        /// </summary>
        Task<SymptomConfigResponse> GetSymptomConfigByIdAsync(int id);

        /// <summary>
        /// Get list of symptom configurations with pagination and filters
        /// </summary>
        Task<PagedData<SymptomConfigResponse>> FilterSymptomConfigsAsync(GetSymptomConfigRequest request);

        /// <summary>
        /// Update an existing symptom configuration
        /// </summary>
        Task<SymptomConfigResponse> UpdateSymptomConfigAsync(int id, UpdateSymptomConfigRequest request);

        /// <summary>
        /// Delete a symptom configuration
        /// </summary>
        Task DeleteSymptomConfigAsync(int id);

        /// <summary>
        /// Get symptom configurations grouped by AttributeKey
        /// </summary>
        Task<Dictionary<string, List<SymptomConfigResponse>>> GetSymptomConfigsGroupedByKeyAsync();

        /// <summary>
        /// Get all symptom configurations without pagination
        /// </summary>
        Task<List<SymptomConfigResponse>> GetAllSymptomConfigAsync();
    }
}
