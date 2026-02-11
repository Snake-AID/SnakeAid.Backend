using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Requests.SnakeCatchingMission;
using SnakeAid.Core.Responses.SnakeCatchingMission;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Service.Implements
{
    public class SnakeCatchingMissionService : ISnakeCatchingMissionService
    {
        private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
        private readonly ILogger<SnakeCatchingMissionService> _logger;

        private decimal basePrice = 500000;

        public SnakeCatchingMissionService(
            IUnitOfWork<SnakeAidDbContext> unitOfWork,
            ILogger<SnakeCatchingMissionService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<SnakeCatchingMissionDetailResponse> StartMissionAsync(
            Guid rescuerId,
            Guid missionId,
            UpdateMissionStatusRequest request)
        {
            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    // Get mission
                    var mission = await _unitOfWork.GetRepository<SnakeCatchingMission>().FirstOrDefaultAsync(
                        predicate: m => m.Id == missionId && m.RescuerId == rescuerId);

                    if (mission == null)
                    {
                        throw new NotFoundException("Mission not found or you don't have permission to access it.");
                    }

                    // Validate current status
                    if (mission.Status != CatchingMissionStatus.Preparing)
                    {
                        throw new BadRequestException($"Cannot start mission. Current status: {mission.Status}. Mission must be in Preparing status.");
                    }

                    // Update to EnRoute
                    mission.Status = CatchingMissionStatus.EnRoute;
                    mission.StartedAt = DateTime.UtcNow;
                    if (!string.IsNullOrWhiteSpace(request.Notes))
                    {
                        mission.Notes = request.Notes;
                    }

                    _unitOfWork.GetRepository<SnakeCatchingMission>().Update(mission);
                    await _unitOfWork.CommitAsync();

                    _logger.LogInformation(
                        "Mission started successfully. MissionId: {MissionId}, RescuerId: {RescuerId}",
                        missionId, rescuerId);

                    return mission.Adapt<SnakeCatchingMissionDetailResponse>();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting mission: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<SnakeCatchingMissionDetailResponse> MarkAsArrivedAsync(
            Guid rescuerId,
            Guid missionId,
            UpdateMissionStatusRequest request)
        {
            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    // Get mission
                    var mission = await _unitOfWork.GetRepository<SnakeCatchingMission>().FirstOrDefaultAsync(
                        predicate: m => m.Id == missionId && m.RescuerId == rescuerId);

                    if (mission == null)
                    {
                        throw new NotFoundException("Mission not found or you don't have permission to access it.");
                    }

                    // Validate current status
                    if (mission.Status != CatchingMissionStatus.EnRoute)
                    {
                        throw new BadRequestException($"Cannot mark as arrived. Current status: {mission.Status}. Mission must be EnRoute.");
                    }

                    // Update to Arrived
                    mission.Status = CatchingMissionStatus.Arrived;
                    mission.ArrivedAt = DateTime.UtcNow;
                    if (!string.IsNullOrWhiteSpace(request.Notes))
                    {
                        mission.Notes = request.Notes;
                    }

                    _unitOfWork.GetRepository<SnakeCatchingMission>().Update(mission);
                    await _unitOfWork.CommitAsync();

                    _logger.LogInformation(
                        "Mission marked as arrived. MissionId: {MissionId}, RescuerId: {RescuerId}",
                        missionId, rescuerId);

                    return mission.Adapt<SnakeCatchingMissionDetailResponse>();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking mission as arrived: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<SnakeCatchingMissionDetailResponse> CompleteMissionAsync(
            Guid rescuerId,
            Guid missionId,
            UpdateMissionStatusRequest request)
        {
            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    // Get mission with related data
                    var mission = await _unitOfWork.GetRepository<SnakeCatchingMission>().FirstOrDefaultAsync(
                        predicate: m => m.Id == missionId && m.RescuerId == rescuerId,
                        include: q => q
                            .Include(m => m.SnakeCatchingRequest)
                            .ThenInclude(r => r.Media)
                            .Include(m => m.MissionDetails)
                            .ThenInclude(d => d.SnakeSpecies));

                    if (mission == null)
                    {
                        throw new NotFoundException("Mission not found or you don't have permission to access it.");
                    }

                    // Validate current status
                    if (mission.Status != CatchingMissionStatus.Arrived)
                    {
                        throw new BadRequestException($"Cannot complete mission. Current status: {mission.Status}. Mission must be in Arrived status.");
                    }

                    // Validate: Check if SnakeCatchingRequest has evidence media
                    //var hasEvidence = mission.SnakeCatchingRequest.Media
                    //    .Any(m => m.Purpose == MediaPurpose.Evidence && m.ReferenceType == MediaReferenceType.SnakeCatchingRequest);

                    //if (!hasEvidence)
                    //{
                    //    throw new BadRequestException("Cannot complete mission. SnakeCatchingRequest must have at least one evidence media.");
                    //}

                    //Update actual cost if provided
                    decimal additionalCosts = mission.MissionDetails?.Sum(d => d.Quantity * 100000) ?? 0;
                    mission.ActualCost = basePrice + additionalCosts;
                    mission.Price = mission.ActualCost.Value + mission.EstimatedCost.Value;

                    // Update mission to MissionCompleted
                    mission.Status = CatchingMissionStatus.MissionCompleted;
                    mission.CompletedAt = DateTime.UtcNow;
                    if (!string.IsNullOrWhiteSpace(request.Notes))
                    {
                        mission.Notes = request.Notes;
                    }

                    // Update SnakeCatchingRequest to Finished
                    mission.SnakeCatchingRequest.Status = RequestStatus.Finished;

                    _unitOfWork.GetRepository<SnakeCatchingMission>().Update(mission);
                    _unitOfWork.GetRepository<SnakeCatchingRequest>().Update(mission.SnakeCatchingRequest);
                    await _unitOfWork.CommitAsync();

                    _logger.LogInformation(
                        "Mission completed successfully. MissionId: {MissionId}, RescuerId: {RescuerId}, RequestId: {RequestId}",
                        missionId, rescuerId, mission.SnakeCatchingRequestId);

                    // Map to response with mission details
                    var response = mission.Adapt<SnakeCatchingMissionDetailResponse>();
                    
                    // Map mission details if any
                    if (mission.MissionDetails != null && mission.MissionDetails.Any())
                    {
                        response.MissionDetails = mission.MissionDetails.Select(d => new CatchingMissionDetailResponse
                        {
                            Id = d.Id,
                            SnakeCatchingMissionId = d.SnakeCatchingMissionId,
                            SnakeSpeciesId = d.SnakeSpeciesId,
                            SnakeSpeciesName = d.SnakeSpecies?.CommonName,
                            Quantity = d.Quantity,
                            CreatedAt = d.CreatedAt,
                            UpdatedAt = d.UpdatedAt
                        }).ToList();
                    }

                    return response;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing mission: {Message}", ex.Message);
                throw;
            }
        }
    }
}
