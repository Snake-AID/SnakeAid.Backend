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
    }
}
