using SnakeAid.Core.Requests.CatchingEnvironment;
using SnakeAid.Core.Responses.CatchingEnvironment;

namespace SnakeAid.Service.Interfaces
{
    public interface ICatchingEnvironmentService
    {
        /// <summary>
        /// Create a new catching environment
        /// </summary>
        Task<CatchingEnvironmentResponse> CreateCatchingEnvironmentAsync(CreateCatchingEnvironmentRequest request);

        /// <summary>
        /// Get catching environment by ID
        /// </summary>
        Task<CatchingEnvironmentResponse> GetCatchingEnvironmentByIdAsync(int id);

        /// <summary>
        /// Get all catching environments
        /// </summary>
        Task<List<CatchingEnvironmentResponse>> GetAllCatchingEnvironmentsAsync();

        /// <summary>
        /// Update an existing catching environment
        /// </summary>
        Task<CatchingEnvironmentResponse> UpdateCatchingEnvironmentAsync(int id, UpdateCatchingEnvironmentRequest request);

        /// <summary>
        /// Delete a catching environment
        /// </summary>
        Task<bool> DeleteCatchingEnvironmentAsync(int id);
    }
}
