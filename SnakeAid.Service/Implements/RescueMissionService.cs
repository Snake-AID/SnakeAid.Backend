using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Requests.RescueMission;
using SnakeAid.Core.Responses.RescueMission;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Service.Implements
{
    public class RescueMissionService : IRescueMissionService
    {
        private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
        private readonly ILogger<RescueMissionService> _logger;

        public RescueMissionService(
            IUnitOfWork<SnakeAidDbContext> unitOfWork,
            ILogger<RescueMissionService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<RescueMissionStatusResponse> UpdateMissionStatusAsync(
            Guid missionId, 
            UpdateRescueMissionStatusRequest request, 
            Guid rescuerId)
        {
            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    // Get mission with incident
                    var mission = await _unitOfWork.GetRepository<RescueMission>()
                        .FirstOrDefaultAsync(
                            predicate: m => m.Id == missionId,
                            include: m => m.Include(x => x.Incident));

                    if (mission == null)
                    {
                        throw new NotFoundException($"Mission {missionId} not found");
                    }

                    // Validate that the rescuer owns this mission
                    if (mission.RescuerId != rescuerId)
                    {
                        throw new ForbiddenException("You are not authorized to update this mission");
                    }

                    var previousStatus = mission.Status;

                    // Validate status transition
                    ValidateStatusTransition(previousStatus, request.Status);

                    // Special handling for MissionCompleted status
                    if (request.Status == RescueMissionStatus.MissionCompleted)
                    {
                        // Require verification images
                        await ValidateVerificationImagesAsync(missionId, request.VerificationImageIds);
                        
                        mission.CompletedAt = DateTime.UtcNow;
                        mission.ActualCost = request.ActualCost;

                        // Update incident status to Finished
                        if (mission.Incident != null)
                        {
                            mission.Incident.Status = SnakebiteIncidentStatus.Finished;
                            _unitOfWork.GetRepository<SnakebiteIncident>().Update(mission.Incident);
                            
                            _logger.LogInformation(
                                "Mission {MissionId} completed. Incident {IncidentId} status changed to Finished",
                                missionId, mission.IncidentId);
                        }
                    }

                    // Validate cancellation reason for Cancelled and Aborted statuses
                    if (request.Status == RescueMissionStatus.Cancelled || request.Status == RescueMissionStatus.MissionAborted)
                    {
                        if (string.IsNullOrWhiteSpace(request.CancellationReason))
                        {
                            throw new BadRequestException($"CancellationReason is required when status is {request.Status}");
                        }
                    }

                    // Update status-specific timestamps
                    UpdateStatusTimestamps(mission, request.Status);

                    // Update mission
                    mission.Status = request.Status;
                    if (!string.IsNullOrWhiteSpace(request.Notes))
                    {
                        mission.Notes = request.Notes;
                    }

                    // Update cancellation reason for Cancelled and Aborted statuses
                    if (request.Status == RescueMissionStatus.Cancelled || request.Status == RescueMissionStatus.MissionAborted)
                    {
                        mission.CancellationReason = request.CancellationReason;
                    }

                    _unitOfWork.GetRepository<RescueMission>().Update(mission);
                    await _unitOfWork.CommitAsync();

                    _logger.LogInformation(
                        "Mission {MissionId} status updated from {PreviousStatus} to {NewStatus} by rescuer {RescuerId}",
                        missionId, previousStatus, request.Status, rescuerId);

                    return await BuildResponseAsync(mission, previousStatus);
                });
            }
            catch (Exception ex) when (ex is not BadRequestException && ex is not ForbiddenException && ex is not NotFoundException)
            {
                _logger.LogError(ex, "Error updating mission {MissionId} status", missionId);
                throw;
            }
        }

        public async Task<RescueMissionStatusResponse> GetMissionDetailsAsync(Guid missionId)
        {
            var mission = await _unitOfWork.GetRepository<RescueMission>()
                .FirstOrDefaultAsync(
                    predicate: m => m.Id == missionId,
                    include: m => m.Include(x => x.Incident));

            if (mission == null)
            {
                throw new NotFoundException($"Mission {missionId} not found");
            }

            return await BuildResponseAsync(mission, mission.Status);
        }

        #region Private Helper Methods

        private void ValidateStatusTransition(RescueMissionStatus currentStatus, RescueMissionStatus newStatus)
        {
            // Allow same status (idempotent)
            if (currentStatus == newStatus)
            {
                return;
            }

            // Define valid transitions
            var validTransitions = currentStatus switch
            {
                RescueMissionStatus.Preparing => new[] { 
                    RescueMissionStatus.EnRoute, 
                    RescueMissionStatus.Cancelled 
                },
                RescueMissionStatus.EnRoute => new[] { 
                    RescueMissionStatus.RescuerArrived, 
                    RescueMissionStatus.MissionAborted 
                },
                RescueMissionStatus.RescuerArrived => new[] { 
                    RescueMissionStatus.MissionCompleted, 
                    RescueMissionStatus.MissionUncompleted, 
                    RescueMissionStatus.MissionAborted 
                },
                // Terminal states - no transitions allowed
                RescueMissionStatus.MissionCompleted => Array.Empty<RescueMissionStatus>(),
                RescueMissionStatus.MissionUncompleted => Array.Empty<RescueMissionStatus>(),
                RescueMissionStatus.MissionAborted => Array.Empty<RescueMissionStatus>(),
                RescueMissionStatus.Cancelled => Array.Empty<RescueMissionStatus>(),
                _ => Array.Empty<RescueMissionStatus>()
            };

            if (!validTransitions.Contains(newStatus))
            {
                throw new BadRequestException(
                    $"Invalid status transition from {currentStatus} to {newStatus}. " +
                    $"Valid transitions: {string.Join(", ", validTransitions)}");
            }
        }

        private void UpdateStatusTimestamps(RescueMission mission, RescueMissionStatus newStatus)
        {
            switch (newStatus)
            {
                case RescueMissionStatus.EnRoute:
                    mission.StartedAt = DateTime.UtcNow;
                    break;
                case RescueMissionStatus.RescuerArrived:
                    mission.ArrivedAt = DateTime.UtcNow;
                    break;
                case RescueMissionStatus.MissionCompleted:
                case RescueMissionStatus.MissionUncompleted:
                case RescueMissionStatus.MissionAborted:
                    if (!mission.CompletedAt.HasValue)
                    {
                        mission.CompletedAt = DateTime.UtcNow;
                    }
                    break;
            }
        }

        private async Task ValidateVerificationImagesAsync(Guid missionId, System.Collections.Generic.List<Guid>? imageIds)
        {
            if (imageIds == null || !imageIds.Any())
            {
                throw new BadRequestException("At least one verification image is required to complete the mission");
            }

            var mediaRepo = _unitOfWork.GetRepository<ReportMedia>();
            var verificationImages = await mediaRepo.GetListAsync(
                predicate: m => imageIds.Contains(m.Id) &&
                               m.ReferenceId == missionId &&
                               m.ReferenceType == MediaReferenceType.RescueMission &&
                               m.Purpose == MediaPurpose.Evidence);

            if (verificationImages.Count != imageIds.Count)
            {
                throw new BadRequestException(
                    $"Invalid verification images. Expected {imageIds.Count} valid images, found {verificationImages.Count}");
            }
        }

        private async Task<RescueMissionStatusResponse> BuildResponseAsync(
            RescueMission mission, 
            RescueMissionStatus previousStatus)
        {
            var response = mission.Adapt<RescueMissionStatusResponse>();
            response.PreviousStatus = previousStatus;
            response.UpdatedAt = DateTime.UtcNow;

            // Get incident status if loaded
            if (mission.Incident != null)
            {
                response.IncidentStatus = mission.Incident.Status;
            }

            // Get verification image count
            var imageCount = await _unitOfWork.GetRepository<ReportMedia>()
                .CountAsync(predicate: m =>
                    m.ReferenceId == mission.Id &&
                    m.ReferenceType == MediaReferenceType.RescueMission &&
                    m.Purpose == MediaPurpose.Evidence);

            response.VerificationImageCount = imageCount;

            return response;
        }

        #endregion
    }
}
