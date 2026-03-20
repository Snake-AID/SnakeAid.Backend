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
        /// Create snake species from excel file (4 sheets) and image upload
        /// </summary>
        Task<DetailSnakeSpeciesResponse> CreateSnakeSpeciesFromExcelAsync(CreateSnakeSpeciesFromExcelRequest request, ClaimsPrincipal user, CancellationToken ct = default);


    }
}
