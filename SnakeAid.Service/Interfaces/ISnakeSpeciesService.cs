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
    }
}
