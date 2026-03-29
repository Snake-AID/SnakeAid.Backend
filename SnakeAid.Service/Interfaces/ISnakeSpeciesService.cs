using System.Security.Claims;
using SnakeAid.Core.Requests.SnakeSpecies;
using SnakeAid.Core.Responses.SnakeSpecies;

namespace SnakeAid.Service.Interfaces
{
    public interface ISnakeSpeciesService
    {
        /// <summary>
        /// Get all snake species
        /// </summary>
        Task<List<ListSnakeSpeciesResponse>> GetAllSnakeSpeciesAsync();

        /// <summary>
        /// Get snake species details by ID
        /// </summary>
        Task<DetailSnakeSpeciesResponse> GetSnakeSpeciesByIdAsync(int id);

        /// <summary>
        /// Search snake species by text query with venom and antivenom data
        /// </summary>
        Task<List<SearchSnakeSpeciesResponse>> SearchSnakeSpeciesAsync(string query);

        /// <summary>
        /// Create snake species
        /// </summary>
        Task<DetailSnakeSpeciesResponse> CreateSnakeSpeciesAsync(CreateSnakeSpeciesRequest request, CancellationToken ct = default);

        /// <summary>
        /// Update snake species
        /// </summary>
        Task<DetailSnakeSpeciesResponse> UpdateSnakeSpeciesAsync(int id, UpdateSnakeSpeciesRequest request, CancellationToken ct = default);

        /// <summary>
        /// Delete snake species
        /// </summary>
        Task DeleteSnakeSpeciesAsync(int id, CancellationToken ct = default);

        /// <summary>
        /// Create snake species from excel file (4 sheets) and image upload
        /// </summary>
        Task<DetailSnakeSpeciesResponse> CreateSnakeSpeciesWithFileAsync(CreateSnakeSpeciesWithFileRequest request, ClaimsPrincipal user, CancellationToken ct = default);

        /// <summary>
        /// Filter snake species by questionnaire answers
        /// </summary>
        Task<List<FilteredSnakeResponse>> FilterSnakesByAnswersAsync(List<int> selectedOptionIds, CancellationToken ct = default);

        /// <summary>
        /// Get snakes by GPS location (location-based filtering)
        /// </summary>
        Task<SnakesByLocationResponse> GetSnakesByLocationAsync(double lat, double lng, CancellationToken ct = default);
    }
}
