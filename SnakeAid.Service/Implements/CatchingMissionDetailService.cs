using Mapster;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Requests.SnakeCatchingMission;
using SnakeAid.Core.Responses.SnakeCatchingMission;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;
using System;
using System.Threading.Tasks;

namespace SnakeAid.Service.Implements
{
    public class CatchingMissionDetailService : ICatchingMissionDetailService
    {
        private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
        private readonly ILogger<CatchingMissionDetailService> _logger;

        public CatchingMissionDetailService(
            IUnitOfWork<SnakeAidDbContext> unitOfWork,
            ILogger<CatchingMissionDetailService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<CatchingMissionDetailResponse> CreateCatchingMissionDetailAsync(CreateCatchingMissionDetailRequest request)
        {
            try
            {
                if (request == null)
                {
                    throw new BadRequestException("Request data cannot be null.");
                }

                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    // Validate SnakeCatchingMission exists
                    var mission = await _unitOfWork.GetRepository<SnakeCatchingMission>()
                        .FirstOrDefaultAsync(predicate: m => m.Id == request.SnakeCatchingMissionId);

                    if (mission == null)
                    {
                        throw new NotFoundException($"Snake catching mission with ID {request.SnakeCatchingMissionId} not found.");
                    }

                    // Validate SnakeSpecies exists
                    var snakeSpecies = await _unitOfWork.GetRepository<SnakeSpecies>()
                        .FirstOrDefaultAsync(predicate: s => s.Id == request.SnakeSpeciesId);

                    if (snakeSpecies == null)
                    {
                        throw new NotFoundException($"Snake species with ID {request.SnakeSpeciesId} not found.");
                    }

                    // Create new CatchingMissionDetail
                    var missionDetail = new CatchingMissionDetail
                    {
                        Id = Guid.NewGuid(),
                        SnakeCatchingMissionId = request.SnakeCatchingMissionId,
                        SnakeSpeciesId = request.SnakeSpeciesId,
                        Quantity = request.Quantity,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _unitOfWork.GetRepository<CatchingMissionDetail>().InsertAsync(missionDetail);
                    await _unitOfWork.CommitAsync();

                    _logger.LogInformation(
                        "Catching mission detail created successfully. DetailId: {DetailId}, MissionId: {MissionId}, SpeciesId: {SpeciesId}",
                        missionDetail.Id, request.SnakeCatchingMissionId, request.SnakeSpeciesId);

                    // Map to response and include snake species name
                    var response = missionDetail.Adapt<CatchingMissionDetailResponse>();
                    response.SnakeSpeciesName = snakeSpecies.CommonName;

                    return response;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating catching mission detail: {Message}", ex.Message);
                throw;
            }
        }
    }
}
