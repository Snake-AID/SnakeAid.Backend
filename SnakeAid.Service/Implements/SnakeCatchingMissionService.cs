using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Constants;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Requests.PayOs;
using SnakeAid.Core.Requests.SnakeCatchingMission;
using SnakeAid.Core.Responses.CatchingEnvironment;
using SnakeAid.Core.Responses.Media;
using SnakeAid.Core.Responses.SnakeCatchingMission;
using SnakeAid.Core.Services;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Extensions;
using SnakeAid.Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Service.Implements
{
    public class SnakeCatchingMissionService : ISnakeCatchingMissionService
    {
        private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
        private readonly ILogger<SnakeCatchingMissionService> _logger;
        private readonly ISystemSettingService _systemSettingService;
        private readonly ISnakeCatchingPaymentService _snakeCatchingPaymentService;
        private readonly IRescuerOnlineStatusService _rescuerOnlineStatusService;
        private readonly ISnakeCatchingRequestNotificationService _snakeCatchingRequestNotificationService;

        private const decimal CATCHING_BASE_PRICE = 4000;
        private const decimal VENOM_SNAKE_PRICE = 100000;
        private const decimal NONVENOM_SNAKE_PRICE = 50000;


        public SnakeCatchingMissionService(
            IUnitOfWork<SnakeAidDbContext> unitOfWork,
            ILogger<SnakeCatchingMissionService> logger,
            ISystemSettingService systemSettingService,
            ISnakeCatchingPaymentService snakeCatchingPaymentService,
            IRescuerOnlineStatusService rescuerOnlineStatusService,
            ISnakeCatchingRequestNotificationService snakeCatchingRequestNotificationService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _systemSettingService = systemSettingService;
            _snakeCatchingPaymentService = snakeCatchingPaymentService;
            _rescuerOnlineStatusService = rescuerOnlineStatusService;
            _snakeCatchingRequestNotificationService = snakeCatchingRequestNotificationService;
        }

        private async Task SafeNotifyAsync(Func<Task> notifyAction, string operationName)
        {
            try
            {
                await notifyAction();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Notification failed during {OperationName}: {Message}", operationName, ex.Message);
            }
        }

        private static decimal GetSnakeUnitPrice(
            CatchingMissionDetail detail,
            decimal venomSnakePrice,
            decimal nonVenomSnakePrice)
        {
            return detail.SnakeSpecies?.IsVenomous == true
                ? venomSnakePrice
                : nonVenomSnakePrice;
        }

        public async Task<SnakeCatchingMissionDetailResponse> StartMissionAsync(
            Guid rescuerId,
            Guid missionId,
            UpdateMissionStatusRequest request)
        {
            try
            {
                var (response, requestId, memberUserId, rescuerName) = await _unitOfWork.ExecuteInTransactionAsync(async () =>
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

                    // Mark rescuer as in mission in status table (for operator map and dispatch filters)
                    await _rescuerOnlineStatusService.SetInMissionAsync(rescuerId.ToString());

                    await _unitOfWork.CommitAsync();

                    _logger.LogInformation(
                        "Mission started successfully. MissionId: {MissionId}, RescuerId: {RescuerId}",
                        missionId, rescuerId);

                    var requestInfo = await _unitOfWork.GetRepository<SnakeCatchingRequest>()
                        .FirstOrDefaultAsync(
                            selector: r => new { r.Id, r.UserId },
                            predicate: r => r.Id == mission.SnakeCatchingRequestId);

                    var rescuerName = await _unitOfWork.GetRepository<Account>()
                        .FirstOrDefaultAsync(selector: a => a.FullName, predicate: a => a.Id == rescuerId);

                    await mission.AttachReportMediaAsync(_unitOfWork, MediaReferenceType.SnakeCatchingMission);
                    var response = mission.Adapt<SnakeCatchingMissionDetailResponse>();
                    return (response, requestInfo?.Id, requestInfo?.UserId, rescuerName);
                });

                if (requestId.HasValue && memberUserId.HasValue)
                {
                    await SafeNotifyAsync(
                        () => _snakeCatchingRequestNotificationService.NotifyMissionEnRouteAsync(
                            requestId.Value,
                            missionId,
                            memberUserId.Value,
                            rescuerId,
                            rescuerName),
                        nameof(_snakeCatchingRequestNotificationService.NotifyMissionEnRouteAsync));
                }

                return response;
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
                var (response, requestId, memberUserId, rescuerName) = await _unitOfWork.ExecuteInTransactionAsync(async () =>
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

                    var requestInfo = await _unitOfWork.GetRepository<SnakeCatchingRequest>()
                        .FirstOrDefaultAsync(
                            selector: r => new { r.Id, r.UserId },
                            predicate: r => r.Id == mission.SnakeCatchingRequestId);

                    var rescuerName = await _unitOfWork.GetRepository<Account>()
                        .FirstOrDefaultAsync(selector: a => a.FullName, predicate: a => a.Id == rescuerId);

                    await mission.AttachReportMediaAsync(_unitOfWork, MediaReferenceType.SnakeCatchingMission);
                    var response = mission.Adapt<SnakeCatchingMissionDetailResponse>();
                    return (response, requestInfo?.Id, requestInfo?.UserId, rescuerName);
                });

                if (requestId.HasValue && memberUserId.HasValue)
                {
                    await SafeNotifyAsync(
                        () => _snakeCatchingRequestNotificationService.NotifyMissionArrivedAsync(
                            requestId.Value,
                            missionId,
                            memberUserId.Value,
                            rescuerId,
                            rescuerName),
                        nameof(_snakeCatchingRequestNotificationService.NotifyMissionArrivedAsync));
                }

                return response;
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
                var basePrice = _systemSettingService.GetSetting(SystemSettingKeys.CatchingBasePrice, CATCHING_BASE_PRICE);
                var venomSnakePrice = _systemSettingService.GetSetting(SystemSettingKeys.CatchingVenomSnakePrice, VENOM_SNAKE_PRICE);
                var nonVenomSnakePrice = _systemSettingService.GetSetting(SystemSettingKeys.CatchingNonVenomSnakePrice, NONVENOM_SNAKE_PRICE);

                var (response, requestId, memberUserId, rescuerName, actualCost) = await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    // Get mission with related data
                    var mission = await _unitOfWork.GetRepository<SnakeCatchingMission>().FirstOrDefaultAsync(
                        predicate: m => m.Id == missionId && m.RescuerId == rescuerId,
                        include: q => q
                            .Include(m => m.SnakeCatchingRequest)
                            .Include(m => m.CatchingEnvironment)
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

                    var hasEvidence = await _unitOfWork.GetRepository<ReportMedia>()
                        .ExistsAsync(m => m.ReferenceId == missionId
                            && m.ReferenceType == MediaReferenceType.SnakeCatchingMission
                            && m.Purpose == MediaPurpose.Evidence);

                    if (!hasEvidence)
                    {
                        throw new BadRequestException("Cannot complete mission. SnakeCatchingMission must have at least one evidence media.");
                    }

                    // Update catching environment if provided
                    decimal envCost = 0;

                    if (request.CatchingEnvironmentId.HasValue)
                    {
                        // Validate catching environment exists
                        var catchingEnvExists = await _unitOfWork.GetRepository<CatchingEnvironment>()
                            .FirstOrDefaultAsync(predicate: ce => ce.Id == request.CatchingEnvironmentId.Value);

                        if (catchingEnvExists == null)
                        {
                            throw new NotFoundException($"Catching environment with ID {request.CatchingEnvironmentId.Value} not found.");
                        }

                        envCost = catchingEnvExists.Price;

                        mission.CatchingEnvironmentId = request.CatchingEnvironmentId.Value;
                        mission.CatchingEnvironment = catchingEnvExists;
                    }

                    var missionDetails = mission.MissionDetails?.ToList() ?? new List<CatchingMissionDetail>();
                    var additionalCosts = missionDetails.Sum(d => d.Quantity * GetSnakeUnitPrice(d, venomSnakePrice, nonVenomSnakePrice));
                    mission.ActualCost = missionDetails.Count > 0
                        ? basePrice + additionalCosts + envCost
                        : 0;

                    mission.Price = basePrice;

                    // Update mission to MissionCompleted
                    mission.Status = CatchingMissionStatus.MissionCompleted;
                    mission.CompletedAt = DateTime.UtcNow;
                    if (!string.IsNullOrWhiteSpace(request.Notes))
                    {
                        mission.Notes = request.Notes;
                    }

                    // Update mission first
                    _unitOfWork.GetRepository<SnakeCatchingMission>().Update(mission);

                    // Update SnakeCatchingRequest to Finished
                    var catchingRequest = await _unitOfWork.GetRepository<SnakeCatchingRequest>()
                        .FirstOrDefaultAsync(predicate: r => r.Id == mission.SnakeCatchingRequestId);

                    if (catchingRequest != null)
                    {
                        catchingRequest.Status = RequestStatus.Finished;
                        _unitOfWork.GetRepository<SnakeCatchingRequest>().Update(catchingRequest);
                    }

                    await _unitOfWork.CommitAsync();

                    _logger.LogInformation(
                        "Mission completed successfully. MissionId: {MissionId}, RescuerId: {RescuerId}, RequestId: {RequestId}",
                        missionId, rescuerId, mission.SnakeCatchingRequestId);

                    var requestInfo = await _unitOfWork.GetRepository<SnakeCatchingRequest>()
                        .FirstOrDefaultAsync(
                            selector: r => new { r.Id, r.UserId },
                            predicate: r => r.Id == mission.SnakeCatchingRequestId);

                    var rescuerName = await _unitOfWork.GetRepository<Account>()
                        .FirstOrDefaultAsync(selector: a => a.FullName, predicate: a => a.Id == rescuerId);

                    await mission.AttachReportMediaAsync(_unitOfWork, MediaReferenceType.SnakeCatchingMission);
                    var response = mission.Adapt<SnakeCatchingMissionDetailResponse>();

                    if (mission.CatchingEnvironment != null)
                    {
                        response.CatchingEnvironment = mission.CatchingEnvironment.Adapt<CatchingEnvironmentResponse>();
                    }

                    if (missionDetails.Any())
                    {
                        response.MissionDetails = missionDetails.Select(d => new CatchingMissionDetailResponse
                        {
                            Id = d.Id,
                            SnakeCatchingMissionId = d.SnakeCatchingMissionId,
                            SnakeSpeciesId = d.SnakeSpeciesId,
                            SnakeSpeciesName = d.SnakeSpecies?.CommonName,
                            Quantity = d.Quantity,
                            Price = d.Quantity * GetSnakeUnitPrice(d, venomSnakePrice, nonVenomSnakePrice),
                            CreatedAt = d.CreatedAt,
                            UpdatedAt = d.UpdatedAt
                        }).ToList();
                    }

                    return (response, requestInfo?.Id, requestInfo?.UserId, rescuerName, mission.ActualCost);
                });

                if (requestId.HasValue && memberUserId.HasValue)
                {
                    await SafeNotifyAsync(
                        () => _snakeCatchingRequestNotificationService.NotifyMissionCompletedAsync(
                            requestId.Value,
                            missionId,
                            memberUserId.Value,
                            rescuerId,
                            rescuerName,
                            actualCost),
                        nameof(_snakeCatchingRequestNotificationService.NotifyMissionCompletedAsync));
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing mission: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<SnakeCatchingMissionDetailResponse> UncompleteMissionAsync(
            Guid rescuerId,
            Guid missionId,
            UncompleteSnakeCatchingMissionRequest request)
        {
            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var mission = await _unitOfWork.GetRepository<SnakeCatchingMission>().FirstOrDefaultAsync(
                        predicate: m => m.Id == missionId && m.RescuerId == rescuerId,
                        include: q => q.Include(m => m.SnakeCatchingRequest));

                    if (mission == null)
                    {
                        throw new NotFoundException("Mission not found or you don't have permission to access it.");
                    }

                    if (mission.Status != CatchingMissionStatus.Arrived)
                    {
                        throw new BadRequestException($"Cannot mark mission as uncompleted. Current status: {mission.Status}. Mission must be in Arrived status.");
                    }

                    var hasEvidence = await _unitOfWork.GetRepository<ReportMedia>()
                        .ExistsAsync(m => m.ReferenceId == mission.SnakeCatchingRequestId
                            && m.ReferenceType == MediaReferenceType.SnakeCatchingRequest
                            && m.Purpose == MediaPurpose.Evidence);

                    if (!hasEvidence)
                    {
                        throw new BadRequestException("Cannot mark mission as uncompleted. SnakeCatchingRequest must have at least one evidence media.");
                    }

                    mission.Status = CatchingMissionStatus.MissionUncompleted;
                    mission.CancellationReason = request.Reason;
                    mission.CompletedAt = DateTime.UtcNow;
                    _unitOfWork.GetRepository<SnakeCatchingMission>().Update(mission);

                    var catchingRequest = await _unitOfWork.GetRepository<SnakeCatchingRequest>()
                        .FirstOrDefaultAsync(predicate: r => r.Id == mission.SnakeCatchingRequestId);

                    if (catchingRequest != null)
                    {
                        catchingRequest.Status = RequestStatus.Completed;
                        _unitOfWork.GetRepository<SnakeCatchingRequest>().Update(catchingRequest);
                    }

                    await _unitOfWork.CommitAsync();

                    _logger.LogInformation(
                        "Mission marked as uncompleted. MissionId: {MissionId}, RescuerId: {RescuerId}, RequestId: {RequestId}, Reason: {Reason}",
                        missionId, rescuerId, mission.SnakeCatchingRequestId, request.Reason);

                    var requestInfo = await _unitOfWork.GetRepository<SnakeCatchingRequest>()
                        .FirstOrDefaultAsync(
                            selector: r => new { r.Id, r.UserId },
                            predicate: r => r.Id == mission.SnakeCatchingRequestId);

                    var rescuerName = await _unitOfWork.GetRepository<Account>()
                        .FirstOrDefaultAsync(selector: a => a.FullName, predicate: a => a.Id == rescuerId);

                    if (requestInfo != null)
                    {
                        await _snakeCatchingRequestNotificationService.NotifyMissionUncompletedAsync(
                            requestInfo.Id,
                            mission.Id,
                            requestInfo.UserId,
                            rescuerId,
                            rescuerName,
                            request.Reason);
                    }

                    await mission.AttachReportMediaAsync(_unitOfWork, MediaReferenceType.SnakeCatchingMission);
                    return mission.Adapt<SnakeCatchingMissionDetailResponse>();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uncompleting mission: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<SnakeCatchingMissionDetailResponse> AbortMissionAsync(
            Guid userId,
            string userRole,
            Guid missionId,
            AbortSnakeCatchingMissionRequest request)
        {
            try
            {
                var (response, requestId, memberUserId, operatorUserId, rescuerName) = await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    // Get mission with related request
                    var mission = await _unitOfWork.GetRepository<SnakeCatchingMission>().FirstOrDefaultAsync(
                        predicate: m => m.Id == missionId,
                        include: q => q.Include(m => m.SnakeCatchingRequest));

                    if (mission == null)
                    {
                        throw new NotFoundException("Mission not found.");
                    }

                    var isOperator = string.Equals(userRole, "Operator", StringComparison.OrdinalIgnoreCase);
                    if (!isOperator && mission.RescuerId != userId)
                    {
                        throw new BadRequestException("You are not authorized to abort this mission.");
                    }

                    // Validate current status - only allow abort from Preparing or EnRoute
                    if (mission.Status != CatchingMissionStatus.Preparing && mission.Status != CatchingMissionStatus.EnRoute)
                    {
                        throw new BadRequestException($"Cannot abort mission. Current status: {mission.Status}. Mission can only be aborted from Preparing or EnRoute status.");
                    }

                    // Update mission to MissionAborted
                    mission.Status = CatchingMissionStatus.MissionAborted;
                    mission.CancellationReason = request.Reason;
                    _unitOfWork.GetRepository<SnakeCatchingMission>().Update(mission);

                    // Reset SnakeCatchingRequest to Pending status
                    var catchingRequest = await _unitOfWork.GetRepository<SnakeCatchingRequest>()
                        .FirstOrDefaultAsync(predicate: r => r.Id == mission.SnakeCatchingRequestId);

                    if (catchingRequest != null)
                    {
                        catchingRequest.Status = RequestStatus.Confirmed;
                        catchingRequest.AssignedRescuerId = null;
                        catchingRequest.AssignedAt = null;
                        _unitOfWork.GetRepository<SnakeCatchingRequest>().Update(catchingRequest);

                        _logger.LogInformation(
                            "Snake catching request reset to Pending. RequestId: {RequestId}",
                            catchingRequest.Id);
                    }

                    await _unitOfWork.CommitAsync();

                    _logger.LogInformation(
                        "Mission aborted successfully. MissionId: {MissionId}, RescuerId: {RescuerId}, RequestId: {RequestId}, Reason: {Reason}",
                        missionId, mission.RescuerId, mission.SnakeCatchingRequestId, request.Reason);

                    var requestInfo = await _unitOfWork.GetRepository<SnakeCatchingRequest>()
                        .FirstOrDefaultAsync(
                            selector: r => new { r.Id, r.UserId, r.HandlingOperatorId },
                            predicate: r => r.Id == mission.SnakeCatchingRequestId);

                    var rescuerName = await _unitOfWork.GetRepository<Account>()
                        .FirstOrDefaultAsync(selector: a => a.FullName, predicate: a => a.Id == mission.RescuerId);

                    await mission.AttachReportMediaAsync(_unitOfWork, MediaReferenceType.SnakeCatchingMission);
                    var response = mission.Adapt<SnakeCatchingMissionDetailResponse>();
                    return (response, requestInfo?.Id, requestInfo?.UserId, requestInfo?.HandlingOperatorId, rescuerName);
                });

                if (requestId.HasValue && memberUserId.HasValue)
                {
                    await SafeNotifyAsync(
                        () => _snakeCatchingRequestNotificationService.NotifyMissionAbortedAsync(
                            requestId.Value,
                            missionId,
                            memberUserId.Value,
                            response.RescuerId,
                            operatorUserId,
                            rescuerName,
                            request.Reason),
                        nameof(_snakeCatchingRequestNotificationService.NotifyMissionAbortedAsync));
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error aborting mission: {Message}", ex.Message);
                throw;
            }
        }
    }
}
