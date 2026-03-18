using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Responses.SnakeSpecies;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Service.Implements
{
    public class SnakeSpeciesService : ISnakeSpeciesService
    {
        private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
        private readonly ILogger<SnakeSpeciesService> _logger;

        public SnakeSpeciesService(
            IUnitOfWork<SnakeAidDbContext> unitOfWork,
            ILogger<SnakeSpeciesService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<List<ListSnakeSpeciesResponse>> GetAllSnakeSpeciesAsync()
        {
            try
            {
                _logger.LogInformation("Fetching all snake species");

                var snakeSpecies = await _unitOfWork.GetRepository<SnakeSpecies>()
                    .GetListAsync(
                        predicate: s => s.IsActive
                    );

                _logger.LogInformation("Retrieved {Count} snake species", snakeSpecies.Count);

                return snakeSpecies.Adapt<List<ListSnakeSpeciesResponse>>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all snake species");
                throw;
            }
        }

        public async Task<DetailSnakeSpeciesResponse> GetSnakeSpeciesByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("Fetching snake species with ID: {Id}", id);

                var snakeSpecies = await _unitOfWork.GetRepository<SnakeSpecies>()
                    .FirstOrDefaultAsync(
                        predicate: s => s.Id == id && s.IsActive,
                        include: query => query
                            .Include(s => s.AlternativeNames)
                            .Include(s => s.SpeciesAntivenoms)
                                .ThenInclude(sa => sa.Antivenom)
                            .Include(s => s.SpeciesVenoms)
                                .ThenInclude(sv => sv.VenomType)
                    );

                if (snakeSpecies == null)
                {
                    _logger.LogWarning("Snake species with ID {Id} not found", id);
                    throw new NotFoundException($"Snake species with ID {id} not found.");
                }

                _logger.LogInformation("Successfully retrieved snake species with ID: {Id}", id);

                return snakeSpecies.Adapt<DetailSnakeSpeciesResponse>();
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching snake species with ID: {Id}", id);
                throw;
            }
        }

        public async Task<List<SearchSnakeSpeciesResponse>> SearchSnakeSpeciesAsync(string query)
        {
            try
            {
                _logger.LogInformation("Searching snake species with query: {Query}", query);

                if (string.IsNullOrWhiteSpace(query))
                {
                    return new List<SearchSnakeSpeciesResponse>();
                }

                var snakeSpecies = await _unitOfWork.GetRepository<SnakeSpecies>()
                    .GetListAsync(
                        predicate: s => s.IsActive &&
                            (s.ScientificName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                             s.CommonName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                             s.AlternativeNames.Any(sn => sn.Name.Contains(query, StringComparison.OrdinalIgnoreCase))),
                        include: query => query
                            .Include(s => s.SpeciesVenoms)
                                .ThenInclude(sv => sv.VenomType)
                            .Include(s => s.SpeciesAntivenoms)
                                .ThenInclude(sa => sa.Antivenom)
                    );

                var result = snakeSpecies.Select(s => new SearchSnakeSpeciesResponse
                {
                    Id = s.Id,
                    ScientificName = s.ScientificName,
                    CommonName = s.CommonName,
                    ImageUrl = s.ImageUrl,
                    IsVenomous = s.IsVenomous,
                    PrimaryVenomType = s.PrimaryVenomType,
                    Venoms = s.SpeciesVenoms.Select(sv => new Core.Responses.SnakeSpecies.VenomInfo
                    {
                        VenomType = sv.VenomType?.Name ?? "Unknown",
                        Description = sv.VenomType?.Description ?? ""
                    }).ToList(),
                    Antivenoms = s.SpeciesAntivenoms.Select(sa => new Core.Responses.SnakeSpecies.AntivenomInfo
                    {
                        AntivenomName = sa.Antivenom?.Name ?? "Unknown",
                        Manufacturer = sa.Antivenom?.Manufacturer ?? "",
                        Effectiveness = sa.Antivenom?.Description ?? ""
                    }).ToList()
                }).ToList();

                _logger.LogInformation("Found {Count} snake species matching query: {Query}", result.Count, query);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching snake species with query: {Query}", query);
                throw;
            }
        }
    }
}
