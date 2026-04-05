using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Requests;
using SnakeAid.Core.Requests.Notification;
using SnakeAid.Core.Requests.SnakebiteIncident;
using SnakeAid.Core.Responses.Media;
using SnakeAid.Core.Responses.SnakebiteIncident;
using SnakeAid.Core.Utils;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;
using SnakeAid.Service.Extensions;
using SnakeAid.Core.Responses.SnakeSpecies;
using SnakeAid.Core.Meta;

namespace SnakeAid.Service.Implements
{
    public class SnakebiteIncidentService : ISnakebiteIncidentService
    {
        private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
        private readonly ILogger<SnakebiteIncidentService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IOperatorRealtimeNotificationService _operatorRealtimeNotificationService;
        private readonly IRescueNotificationService _rescueNotificationService;
        private readonly IMissionNotificationService _missionNotificationService;
        private readonly ISnakeRescueMissionService _snakeRescueMissionService;

        public SnakebiteIncidentService(
            IUnitOfWork<SnakeAidDbContext> unitOfWork,
            ILogger<SnakebiteIncidentService> logger,
            IConfiguration configuration,
            IOperatorRealtimeNotificationService operatorRealtimeNotificationService,
            IRescueNotificationService rescueNotificationService,
            IMissionNotificationService missionNotificationService,
            ISnakeRescueMissionService SnakeRescueMissionService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _configuration = configuration;
            _operatorRealtimeNotificationService = operatorRealtimeNotificationService;
            _rescueNotificationService = rescueNotificationService;
            _missionNotificationService = missionNotificationService;
            _snakeRescueMissionService = SnakeRescueMissionService;
        }

        public async Task<CreateIncidentResponse> ConfirmIncidentAsync(Guid incidentId, Guid operatorId)
        {
            try
            {
                var isNewClaim = false;
                var response = await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var incident = await _unitOfWork.GetRepository<SnakebiteIncident>().FirstOrDefaultAsync(
                        predicate: i => i.Id == incidentId
                    );

                    if (incident == null)
                    {
                        throw new NotFoundException("Snakebite incident not found.");
                    }

                    if (!incident.HandlingOperatorId.HasValue)
                    {
                        incident.HandlingOperatorId = operatorId;
                        isNewClaim = true;
                    }
                    else if (incident.HandlingOperatorId != operatorId)
                    {
                        throw new ConflictException("Incident is being handled by another operator.");
                    }

                    if (incident.Status != SnakebiteIncidentStatus.Pending && incident.Status != SnakebiteIncidentStatus.Verified)
                    {
                        throw new BadRequestException($"Cannot confirm incident with status: {incident.Status}");
                    }

                    if (incident.Status != SnakebiteIncidentStatus.Verified)
                    {
                        incident.Status = SnakebiteIncidentStatus.Verified;
                        incident.ConfirmedAt = DateTime.UtcNow;
                        _unitOfWork.GetRepository<SnakebiteIncident>().Update(incident);
                    }

                    return incident.Adapt<CreateIncidentResponse>();
                });

                if (isNewClaim)
                {
                    await _operatorRealtimeNotificationService.NotifyIncidentClaimedAsync(incidentId, operatorId);
                }

                return response;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict while confirming incident {IncidentId}", incidentId);
                throw new ConflictException("Incident was updated by another operator. Please refresh and try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming incident {IncidentId}: {Message}", incidentId, ex.Message);
                throw;
            }
        }

        public async Task<CreateIncidentResponse> MarkIncidentFalseAlarmAsync(Guid incidentId, Guid operatorId, string? reason)
        {
            try
            {
                var response = await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var incident = await _unitOfWork.GetRepository<SnakebiteIncident>().FirstOrDefaultAsync(
                        predicate: i => i.Id == incidentId
                    );

                    if (incident == null)
                    {
                        throw new NotFoundException("Snakebite incident not found.");
                    }

                    if (incident.HandlingOperatorId != operatorId)
                    {
                        throw new ConflictException("Incident is being handled by another operator.");
                    }

                    incident.Status = SnakebiteIncidentStatus.FalseAlarm;
                    incident.OperatorNotes = string.IsNullOrWhiteSpace(incident.OperatorNotes)
                        ? reason
                        : string.Join("\n", incident.OperatorNotes, reason);

                    _unitOfWork.GetRepository<SnakebiteIncident>().Update(incident);

                    return incident.Adapt<CreateIncidentResponse>();
                });

                await _operatorRealtimeNotificationService.NotifyIncidentFalseAlarmAsync(incidentId, operatorId, reason);

                return response;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict while marking incident {IncidentId} as false alarm", incidentId);
                throw new ConflictException("Incident was updated by another operator. Please refresh and try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking incident {IncidentId} as false alarm: {Message}", incidentId, ex.Message);
                throw;
            }
        }

        public async Task<CreateIncidentResponse> ReportIncidentNoAnswerAsync(Guid incidentId, Guid operatorId, bool continueCalling, string? note)
        {
            try
            {
                var response = await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var incident = await _unitOfWork.GetRepository<SnakebiteIncident>().FirstOrDefaultAsync(
                        predicate: i => i.Id == incidentId
                    );

                    if (incident == null)
                    {
                        throw new NotFoundException("Snakebite incident not found.");
                    }

                    if (incident.HandlingOperatorId != operatorId)
                    {
                        throw new ConflictException("Incident is being handled by another operator.");
                    }

                    // Preserve the operator's note, if any
                    incident.OperatorNotes = string.IsNullOrWhiteSpace(incident.OperatorNotes)
                        ? note
                        : string.Join("\n", incident.OperatorNotes, note);

                    if (continueCalling)
                    {
                        // Keep the incident claimed by this operator but stay in Pending state
                        incident.Status = SnakebiteIncidentStatus.Pending;
                    }
                    else
                    {
                        // Release the incident back to the queue for other operators
                        incident.Status = SnakebiteIncidentStatus.Pending;
                        incident.HandlingOperatorId = null;
                    }

                    _unitOfWork.GetRepository<SnakebiteIncident>().Update(incident);
                    return incident.Adapt<CreateIncidentResponse>();
                });

                await _operatorRealtimeNotificationService.NotifyIncidentNoAnswerAsync(incidentId, operatorId, note, continueCalling);

                return response;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict while reporting no-answer for incident {IncidentId}", incidentId);
                throw new ConflictException("Incident was updated by another operator. Please refresh and try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reporting no-answer for incident {IncidentId}: {Message}", incidentId, ex.Message);
                throw;
            }
        }

        public async Task<CreateIncidentResponse> DispatchIncidentAsync(Guid incidentId, Guid rescuerId, Guid operatorId)
        {
            try
            {
                Guid dispatchRequestId = Guid.Empty;
                DateTime dispatchedAt = DateTime.UtcNow;
                double incidentLatitude = 0;
                double incidentLongitude = 0;

                var response = await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var incident = await _unitOfWork.GetRepository<SnakebiteIncident>().FirstOrDefaultAsync(
                        predicate: i => i.Id == incidentId
                    );

                    if (incident == null)
                    {
                        throw new NotFoundException("Snakebite incident not found.");
                    }

                    if (incident.HandlingOperatorId != operatorId)
                    {
                        throw new ConflictException("Incident is being handled by another operator.");
                    }

                    if (incident.Status != SnakebiteIncidentStatus.Verified)
                    {
                        throw new BadRequestException($"Cannot dispatch incident with status: {incident.Status}");
                    }

                    var rescuer = await _unitOfWork.GetRepository<RescuerProfile>().FirstOrDefaultAsync(
                        predicate: r => r.AccountId == rescuerId
                    );

                    if (rescuer == null)
                    {
                        throw new NotFoundException("Rescuer not found.");
                    }

                    var hasDeclinedIncident = await _unitOfWork.GetRepository<RescuerRequest>().CreateBaseQuery(asNoTracking: true)
                        .AnyAsync(r =>
                            r.IncidentId == incidentId
                            && r.RescuerId == rescuer.AccountId
                            && r.Status == RescueRequestStatus.Declined);

                    if (hasDeclinedIncident)
                    {
                        throw new ConflictException("Rescuer already declined this incident and is excluded from re-dispatch.");
                    }

                    var hasAbortedMission = await _unitOfWork.GetRepository<RescueMission>().CreateBaseQuery(asNoTracking: true)
                        .AnyAsync(m =>
                            m.IncidentId == incidentId
                            && m.RescuerId == rescuer.AccountId
                            && m.Status == RescueMissionStatus.MissionAborted);

                    if (hasAbortedMission)
                    {
                        throw new ConflictException("Rescuer already aborted this incident and is excluded from re-dispatch.");
                    }

                    if (!rescuer.IsOnline)
                    {
                        throw new BadRequestException("Rescuer is currently offline.");
                    }

                    if (!rescuer.IsAvailable)
                    {
                        throw new BadRequestException("Rescuer is currently unavailable.");
                    }

                    var nowLocal = AppTime.NowLocal;
                    var isOnDutyNow = await _unitOfWork.GetRepository<ShiftAssignment>().CreateBaseQuery(asNoTracking: true)
                        .AnyAsync(a => a.RescuerId == rescuer.AccountId
                                       && (a.Status == ShiftAssignmentStatus.Scheduled || a.Status == ShiftAssignmentStatus.Active)
                                       && a.ShiftStartLocal <= nowLocal
                                       && a.ShiftEndLocal >= nowLocal);

                    if (!isOnDutyNow)
                    {
                        throw new BadRequestException("Rescuer is not currently on shift.");
                    }

                    // // Ensure only one active pending dispatch request exists per incident
                    // var existingPendingRequests = await _unitOfWork.GetRepository<RescuerRequest>().GetListAsync(
                    //     predicate: r => r.IncidentId == incidentId && r.Status == RescueRequestStatus.Pending);

                    // foreach (var pending in existingPendingRequests)
                    // {
                    //     pending.Status = RescueRequestStatus.Cancelled;
                    //     pending.ResponseAt = DateTime.UtcNow;
                    //     pending.DeclineReason = "Auto-cancelled due to new dispatch request.";
                    //     _unitOfWork.GetRepository<RescuerRequest>().Update(pending);
                    // }

                    var dispatchRequest = new RescuerRequest
                    {
                        Id = Guid.NewGuid(),
                        IncidentId = incidentId,
                        RescuerId = rescuer.AccountId,
                        OperatorId = operatorId,
                        Status = RescueRequestStatus.Pending,
                        DispatchedAt = dispatchedAt,
                        ResponseAt = null,
                        DeclineReason = null
                    };

                    await _unitOfWork.GetRepository<RescuerRequest>().InsertAsync(dispatchRequest);

                    // Keep incident in Verified until rescuer acknowledges the dispatch.
                    incident.Status = SnakebiteIncidentStatus.Verified;
                    incident.DispatchedAt = dispatchedAt;
                    _unitOfWork.GetRepository<SnakebiteIncident>().Update(incident);

                    dispatchRequestId = dispatchRequest.Id;
                    incidentLatitude = incident.LocationCoordinates.Y;
                    incidentLongitude = incident.LocationCoordinates.X;

                    return incident.Adapt<CreateIncidentResponse>();
                });

                await _rescueNotificationService.NotifyDispatchRequestedAsync(rescuerId.ToString(), new DispatchRequestNotificationPayload
                {
                    RequestId = dispatchRequestId,
                    IncidentId = incidentId,
                    OperatorId = operatorId,
                    RescuerId = rescuerId,
                    DispatchedAt = dispatchedAt,
                    Latitude = incidentLatitude,
                    Longitude = incidentLongitude,
                    Message = "Operator assigned a dispatch request. Please acknowledge if you can take this case."
                });

                // Notify operators that we've successfully dispatched the request to a rescuer.
                await _operatorRealtimeNotificationService.NotifyDispatchRequestedAsync(incidentId, rescuerId, operatorId);

                // Add dispatch metadata to response so the caller can verify send success.
                response.DispatchRequestId = dispatchRequestId;
                response.DispatchedRescuerId = rescuerId;

                return response;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict while dispatching incident {IncidentId}", incidentId);
                throw new ConflictException("Incident was updated by another operator. Please refresh and try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dispatching incident {IncidentId}: {Message}", incidentId, ex.Message);
                throw;
            }
        }

        public async Task<AcceptRescueResponse> AcceptDispatchRequestAsync(Guid requestId, Guid rescuerId)
        {
            try
            {
                Guid memberUserId = Guid.Empty;

                var response = await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var request = await _unitOfWork.GetRepository<RescuerRequest>().FirstOrDefaultAsync(
                        predicate: r => r.Id == requestId,
                        include: q => q.Include(r => r.Incident)
                    );

                    if (request == null)
                        throw new NotFoundException("Dispatch request not found.");

                    if (request.RescuerId != rescuerId)
                        throw new ConflictException("This dispatch request does not belong to you.");

                    if (request.Status != RescueRequestStatus.Pending)
                        throw new ConflictException("Dispatch request is not in a pending state.");

                    var incident = request.Incident;
                    if (incident == null)
                        throw new NotFoundException("Associated incident not found.");

                    if (incident.Status != SnakebiteIncidentStatus.Verified)
                        throw new BadRequestException($"Cannot accept dispatch when incident is in status: {incident.Status}");

                    memberUserId = incident.UserId;

                    var mission = await _snakeRescueMissionService.CreateMissionAsync(incident.Id, rescuerId);

                    // Detach loaded navigation object to avoid EF track conflict (same Incident loaded in CreateMissionAsync)
                    request.Incident = null;

                    // Update request status only
                    request.Status = RescueRequestStatus.Accepted;
                    request.ResponseAt = DateTime.UtcNow;
                    _unitOfWork.GetRepository<RescuerRequest>().UpdateProperties(request, r => r.Status, r => r.ResponseAt);

                    var response = new AcceptRescueResponse
                    {
                        RequestId = request.Id,
                        IncidentId = incident.Id,
                        RescuerId = rescuerId,
                        MissionId = mission.Id,
                        AcceptedAt = DateTime.UtcNow,
                        Message = "Dispatch accepted. Mission created."
                    };

                    return response;
                });

                // Best-effort realtime/push notifications after transaction commit.
                // Do not fail accepted dispatch response if notification pipeline has transient issues.
                try
                {
                    // Notify the rescuer that they have accepted the dispatch
                    await _rescueNotificationService.NotifyRescuerAcceptedAsync(rescuerId.ToString(), response);

                    // Notify mission/member channel that rescuer accepted so member also receives push.
                    if (memberUserId != Guid.Empty)
                    {
                        await _missionNotificationService.NotifyRescuerAcceptedAsync(response.IncidentId, memberUserId, response);
                    }

                    // Notify operators that the rescuer has been dispatched.
                    await _operatorRealtimeNotificationService.NotifyRescuerDispatchedAsync(response.IncidentId, rescuerId);
                }
                catch (Exception notifyEx)
                {
                    _logger.LogWarning(notifyEx,
                        "AcceptDispatchRequest notifications failed after commit for request {RequestId}. Core transaction already committed.",
                        requestId);
                }

                return response;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict while accepting dispatch request {RequestId}", requestId);
                throw new ConflictException("Dispatch request was updated by another process. Please refresh and try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting dispatch request {RequestId}: {Message}", requestId, ex.Message);
                throw;
            }
        }

        public async Task<RejectRescueResponse> DeclineDispatchRequestAsync(Guid requestId, Guid rescuerId, string? reason)
        {
            try
            {
                var (response, incident) = await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var request = await _unitOfWork.GetRepository<RescuerRequest>().FirstOrDefaultAsync(
                        predicate: r => r.Id == requestId,
                        include: q => q.Include(r => r.Incident)
                    );

                    if (request == null)
                        throw new NotFoundException("Dispatch request not found.");

                    if (request.RescuerId != rescuerId)
                        throw new ConflictException("This dispatch request does not belong to you.");

                    if (request.Status != RescueRequestStatus.Pending)
                        throw new ConflictException("Dispatch request is not in a pending state.");

                    var incident = request.Incident;
                    if (incident == null)
                        throw new NotFoundException("Associated incident not found.");



                    // Mark as declined
                    request.Status = RescueRequestStatus.Declined;
                    request.ResponseAt = DateTime.UtcNow;
                    request.DeclineReason = reason;
                    _unitOfWork.GetRepository<RescuerRequest>().Update(request);

                    // Return incident to Verified so operator can dispatch again
                    // Only update if the incident has not already moved into a later state
                    if (incident.Status == SnakebiteIncidentStatus.Verified)
                    {
                        _unitOfWork.GetRepository<SnakebiteIncident>().Update(incident);
                    }
                    else
                    {
                        _logger.LogWarning("DeclineDispatchRequest: incident {IncidentId} is in status {Status}; skipping status reset.",
                            incident.Id, incident.Status);
                    }

                    // Ensure rescuer stays available
                    var rescuerProfile = await _unitOfWork.GetRepository<RescuerProfile>().FirstOrDefaultAsync(
                        predicate: r => r.AccountId == rescuerId,
                        asNoTracking: false);
                    if (rescuerProfile != null)
                    {
                        rescuerProfile.IsAvailable = true;
                        _unitOfWork.GetRepository<RescuerProfile>().Update(rescuerProfile);
                    }

                    var response = new RejectRescueResponse
                    {
                        RequestId = request.Id,
                        RejectedAt = DateTime.UtcNow,
                        Message = string.IsNullOrWhiteSpace(reason) ? "Dispatch request was declined." : reason
                    };
                    return (response, incident);
                });

                // Notify rescuer (caller) that the request has been declined
                await _rescueNotificationService.NotifyRescuerDeclinedAsync(rescuerId.ToString(), response);

                // Notify operators that the rescuer declined the dispatch
                await _operatorRealtimeNotificationService.NotifyRescuerDeclinedAsync(incident.Id, rescuerId, reason);

                return response;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict while declining dispatch request {RequestId}", requestId);
                throw new ConflictException("Dispatch request was updated by another process. Please refresh and try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error declining dispatch request {RequestId}: {Message}", requestId, ex.Message);
                throw;
            }
        }

        public async Task<RejectRescueResponse> CancelDispatchRequestAsync(Guid requestId, Guid operatorId)
        {
            try
            {
                var (response, incident, rescuerId) = await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var request = await _unitOfWork.GetRepository<RescuerRequest>().FirstOrDefaultAsync(
                        predicate: r => r.Id == requestId,
                        include: q => q.Include(r => r.Incident)
                    );

                    if (request == null)
                        throw new NotFoundException("Dispatch request not found.");

                    if (request.Status != RescueRequestStatus.Pending)
                        throw new ConflictException("Dispatch request is not in a pending state.");

                    var incident = request.Incident;
                    if (incident == null)
                        throw new NotFoundException("Associated incident not found.");

                    if (incident.HandlingOperatorId != operatorId)
                        throw new ConflictException("Incident is being handled by another operator.");

                    // Mark as cancelled
                    request.Status = RescueRequestStatus.Cancelled;
                    request.ResponseAt = DateTime.UtcNow;
                    request.DeclineReason = "Cancelled by Operator";
                    _unitOfWork.GetRepository<RescuerRequest>().Update(request);

                    // Return incident to Verified so operator can dispatch again
                    // Only update if the incident has not already moved into a later state
                    if (incident.Status == SnakebiteIncidentStatus.Verified)
                    {
                        _unitOfWork.GetRepository<SnakebiteIncident>().Update(incident);
                    }

                    var response = new RejectRescueResponse
                    {
                        RequestId = request.Id,
                        RejectedAt = DateTime.UtcNow,
                        Message = "Dispatch request was cancelled by operator."
                    };
                    return (response, incident, request.RescuerId);
                });

                // Notify rescuer (caller) that the request has been cancelled
                await _rescueNotificationService.NotifyRequestCancelledAsync(rescuerId.ToString(), requestId);

                // Notify operators that the dispatch was cancelled
                await _operatorRealtimeNotificationService.NotifyRescuerDeclinedAsync(incident.Id, rescuerId, "Cancelled by Operator");

                return response;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict while cancelling dispatch request {RequestId}", requestId);
                throw new ConflictException("Dispatch request was updated by another process. Please refresh and try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling dispatch request {RequestId}: {Message}", requestId, ex.Message);
                throw;
            }
        }

        public async Task<CreateIncidentResponse> CancelIncidentAsync(Guid incidentId, CancelIncidentRequest request)
        {
            var cancelReason = request.Reason ?? "No reason provided";
            List<(string RescuerId, Guid RequestId)> pendingNotifies = new();
            Guid? affectedMissionId = null;
            Guid? affectedMissionRescuerId = null;

            try
            {
                var response = await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var incident = await _unitOfWork.GetRepository<SnakebiteIncident>().FirstOrDefaultAsync(
                        predicate: s => s.Id == incidentId
                    );

                    if (incident == null)
                    {
                        throw new NotFoundException("Snakebite incident not found.");
                    }

                    // Can't cancel already finalized incidents
                    var nonCancellableStatuses = new[]
                    {
                        SnakebiteIncidentStatus.Cancelled,
                        SnakebiteIncidentStatus.Finished,
                        SnakebiteIncidentStatus.NoRescuerFound,
                        SnakebiteIncidentStatus.Disputed,
                        SnakebiteIncidentStatus.Completed,
                        SnakebiteIncidentStatus.FalseAlarm
                    };

                    if (nonCancellableStatuses.Contains(incident.Status))
                    {
                        throw new BadRequestException($"Cannot cancel incident with status: {incident.Status}");
                    }

                    // If a mission has already been created and is in EnRoute/RescuerArrived, reject cancellation
                    var inProgressMission = await _unitOfWork.GetRepository<RescueMission>().FirstOrDefaultAsync(
                        predicate: m => m.IncidentId == incidentId &&
                                        (m.Status == RescueMissionStatus.EnRoute || m.Status == RescueMissionStatus.RescuerArrived)
                    );

                    if (inProgressMission != null)
                    {
                        throw new BadRequestException("Cannot cancel incident while the rescuer is already en-route or has arrived.");
                    }

                    // If a mission has already been created and is in Preparing, cancel it.
                    var activeMission = await _unitOfWork.GetRepository<RescueMission>().FirstOrDefaultAsync(
                        predicate: m => m.IncidentId == incidentId && m.Status == RescueMissionStatus.Preparing,
                        include: q => q.Include(m => m.Rescuer)
                    );

                    if (activeMission != null)
                    {
                        affectedMissionId = activeMission.Id;
                        affectedMissionRescuerId = activeMission.RescuerId;

                        activeMission.Status = RescueMissionStatus.Cancelled;
                        activeMission.CancellationReason = cancelReason;
                        activeMission.UpdatedAt = DateTime.UtcNow;

                        // Reset incident to Cancelled
                        incident.Status = SnakebiteIncidentStatus.Cancelled;
                        incident.AssignedRescuerId = null;
                        incident.AssignedAt = null;
                        incident.DispatchedAt = null;

                        // Free rescuer
                        if (activeMission.Rescuer != null)
                        {
                            activeMission.Rescuer.IsAvailable = true;
                            _unitOfWork.GetRepository<RescuerProfile>().Update(activeMission.Rescuer);
                        }

                        _unitOfWork.GetRepository<RescueMission>().Update(activeMission);
                        _unitOfWork.GetRepository<SnakebiteIncident>().Update(incident);

                        return incident.Adapt<CreateIncidentResponse>();
                    }

                    // No active mission yet: cancel pending dispatch requests
                    var pendingRequests = await _unitOfWork.GetRepository<RescuerRequest>().GetListAsync(
                        predicate: r => r.IncidentId == incidentId && r.Status == RescueRequestStatus.Pending
                    );

                    foreach (var req in pendingRequests)
                    {
                        req.Status = RescueRequestStatus.Cancelled;
                        req.ResponseAt = DateTime.UtcNow;
                        req.DeclineReason = cancelReason;
                        _unitOfWork.GetRepository<RescuerRequest>().Update(req);

                        pendingNotifies.Add((req.RescuerId.ToString(), req.Id));
                    }

                    incident.Status = SnakebiteIncidentStatus.Cancelled;
                    incident.AssignedRescuerId = null;
                    incident.AssignedAt = null;
                    incident.DispatchedAt = null;
                    _unitOfWork.GetRepository<SnakebiteIncident>().Update(incident);

                    return incident.Adapt<CreateIncidentResponse>();
                });

                // Notify operator dashboard that the incident was cancelled by the member
                await _operatorRealtimeNotificationService.NotifyIncidentCancelledAsync(incidentId, cancelReason);

                // Notify any rescuer who had a pending dispatch request
                foreach (var (rescuerId, requestId) in pendingNotifies)
                {
                    await _rescueNotificationService.NotifyRequestCancelledAsync(rescuerId, requestId);
                }

                // If there was an active mission, notify via mission hub as well
                if (affectedMissionId.HasValue && affectedMissionRescuerId.HasValue)
                {
                    await _missionNotificationService.NotifyMissionCancelledAsync(
                        incidentId,
                        affectedMissionRescuerId.Value,
                        cancelReason);
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling snakebite incident: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<CreateIncidentResponse> CreateIncidentAsync(CreateIncidentRequest request, Guid userId)
        {
            try
            {
                if (request == null)
                {
                    throw new BadRequestException("Request data cannot be null.");
                }

                var responseData = await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var existingAccount = await _unitOfWork.GetRepository<Account>().FirstOrDefaultAsync(
                        predicate: a => a.Id == userId,
                        include: m => m.Include(i => i.MemberProfile)
                    );

                if (existingAccount.MemberProfile == null)
                {
                    throw new BadRequestException("Member information could not be found for the current account.");
                }

                // Create Point from lng/lat (PostGIS uses SRID 4326 - WGS84)
                var geometryFactory = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
                var locationPoint = geometryFactory.CreatePoint(new NetTopologySuite.Geometries.Coordinate(request.Lng, request.Lat));

                var newIncident = new SnakebiteIncident
                {
                    Id = Guid.NewGuid(),
                    UserId = existingAccount.Id,
                    LocationCoordinates = locationPoint,
                    Status = SnakebiteIncidentStatus.Pending,
                    IncidentOccurredAt = DateTime.UtcNow,
                    Address = request.Address ?? string.Empty,
                };

                await _unitOfWork.GetRepository<SnakebiteIncident>().InsertAsync(newIncident);

                return newIncident.Adapt<CreateIncidentResponse>();
            });

                // Best-effort realtime notify for operator dashboard map.
                await _operatorRealtimeNotificationService.NotifyNewIncidentCreatedAsync(
                    responseData.Id,
                    userId,
                    request.Lat,
                    request.Lng,
                    request.Address ?? string.Empty);

                return responseData;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating snakebite incident: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<DetailSnakebiteIncidentResponse> GetDetailIncidentAsync(Guid incidentId)
        {
            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var existingIncident = await _unitOfWork.GetRepository<SnakebiteIncident>().FirstOrDefaultAsync(
                            predicate: s => s.Id == incidentId,
                            include: query => query
                                .Include(i => i.User)
                                    .ThenInclude(u => u.Account)
                                .Include(i => i.AssignedRescuer)
                                    .ThenInclude(r => r.Account)
                                .Include(i => i.Missions)
                                .Include(i => i.IdentifiedSnakeSpecies)
                                .Include(i => i.AIRecognitionResult)
                        );

                    if (existingIncident == null)
                    {
                        throw new NotFoundException("Snakebite incident not found.");
                    }

                    await existingIncident.AttachReportMediaAsync(_unitOfWork, MediaReferenceType.SnakebiteIncident);
                    await existingIncident.Missions.AttachReportMediaAsync(_unitOfWork, MediaReferenceType.RescueMission);

                    _logger.LogInformation("Loaded {MediaCount} media items for incident {IncidentId}",
                        existingIncident.Media?.Count ?? 0, incidentId);

                    var responseData = existingIncident.Adapt<DetailSnakebiteIncidentResponse>();

                    responseData.RescueMissionMedia = existingIncident.Missions
                        .OrderByDescending(m => m.CreatedAt)
                        .Select(m => new RescueMissionMediaGroupResponse
                        {
                            MissionId = m.Id,
                            MissionStatus = m.Status,
                            Media = m.Media.Adapt<List<ReportMediaResponse>>()
                        })
                        .ToList();

                    // Map identified snake manually if available
                    if (existingIncident.IdentifiedSnakeSpecies != null)
                    {
                        responseData.IdentifiedSnake = existingIncident.IdentifiedSnakeSpecies.Adapt<SnakeSpeciesResponse>();

                        responseData.IdentificationContext = new Core.Responses.FirstAid.SnakeIdentificationContext
                        {
                            Method = existingIncident.IdentificationMethod,
                            IdentifiedAt = existingIncident.IdentifiedAt ?? DateTime.UtcNow
                        };

                        // Add AI confidence if applicable
                        if (existingIncident.IdentificationMethod == SnakeIdentificationMethod.AIDetection
                            && existingIncident.AIRecognitionResult != null)
                        {
                            responseData.IdentificationContext.AIConfidence = (float)existingIncident.AIRecognitionResult.Confidence;
                        }
                    }

                    _logger.LogInformation("Mapped {MediaCount} media items in response for incident {IncidentId}",
                        responseData.Media?.Count ?? 0, incidentId);

                    return responseData;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving snakebite incident details: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<UpdateSymptomReportResponse> UpdateSymptomReportAsync(Guid incidentId, UpdateSymptomReportRequest request)
        {
            try
            {
                if (request == null)
                {
                    throw new BadRequestException("Request data cannot be null.");
                }

                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var existingIncident = await _unitOfWork.GetRepository<SnakebiteIncident>().FirstOrDefaultAsync(
                            predicate: s => s.Id == incidentId
                        );
                    if (existingIncident == null)
                    {
                        throw new NotFoundException("Snakebite incident not found.");
                    }

                    // using the time by miniute in request if provided, otherwise caculate from time provided in Incident
                    var elapsedMinutes = request.TimeSinceBiteMinutes ?? (existingIncident.IncidentOccurredAt.HasValue
                        ? (int)(DateTime.UtcNow - existingIncident.IncidentOccurredAt.Value).TotalMinutes
                        : 0);

                    // Collect symptom descriptions and calculate severity
                    var reportedSymptoms = new List<ReportSymptom>();
                    var coreSymptomScores = new List<int>();
                    var modifierSymptomScores = new List<int>();

                    foreach (var symptomId in request.SymptomIdList)
                    {
                        var symptom = await _unitOfWork.GetRepository<SymptomConfig>().FirstOrDefaultAsync(
                            predicate: s => s.Id == symptomId
                        );

                        if (symptom != null)
                        {
                            // Add symptom description
                            if (!string.IsNullOrEmpty(symptom.Description))
                            {
                                reportedSymptoms.Add(new ReportSymptom
                                {
                                    SymptomId = symptom.Id,
                                    SymptomName = symptom.Name,
                                    SymptomDescription = symptom.Description
                                });
                            }

                            // Calculate score based on TimeScoreList
                            var score = CalculateScoreByElapsedTime(symptom.TimeScoreList, elapsedMinutes);

                            // Categorize by symptom category
                            if (symptom.Category == SymptomCategory.Core)
                            {
                                coreSymptomScores.Add(score);
                            }
                            else if (symptom.Category == SymptomCategory.Modifier)
                            {
                                modifierSymptomScores.Add(score);
                            }
                        }
                    }

                    // Calculate severity level
                    // Core: take maximum score
                    var severityLevel = 0;
                    if (coreSymptomScores.Any())
                    {
                        severityLevel = coreSymptomScores.Max();
                    }

                    // Modifier: sum all scores
                    if (modifierSymptomScores.Any())
                    {
                        severityLevel += modifierSymptomScores.Sum();
                    }

                    if (severityLevel > 100)
                        severityLevel = 100;

                    // Update symptom report and severity level
                    var jsonOptions = new System.Text.Json.JsonSerializerOptions()
                    {
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                        WriteIndented = false
                    };
                    existingIncident.SymptomsReport = reportedSymptoms;
                    existingIncident.SeverityLevel = severityLevel;
                    _unitOfWork.GetRepository<SnakebiteIncident>().Update(existingIncident);
                    await _unitOfWork.CommitAsync();

                    // Attach media before mapping to response
                    await existingIncident.AttachReportMediaAsync(_unitOfWork, MediaReferenceType.SnakebiteIncident);

                    var responseData = existingIncident.Adapt<UpdateSymptomReportResponse>();
                    return responseData;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating symptom report: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Calculate score based on elapsed time and TimeScoreList ranges
        /// </summary>
        /// <param name="timeScoreList">List of time score ranges</param>
        /// <param name="elapsedMinutes">Minutes elapsed since incident occurred</param>
        /// <returns>Score matching the elapsed time range, or 0 if no match found</returns>
        private int CalculateScoreByElapsedTime(List<TimeScorePoint> timeScoreList, int elapsedMinutes)
        {
            if (timeScoreList == null || !timeScoreList.Any())
            {
                return 0;
            }

            // Find the TimeScorePoint where elapsedMinutes falls within MinMinutes and MaxMinutes
            var matchingScore = timeScoreList.FirstOrDefault(ts =>
                elapsedMinutes >= ts.MinMinutes && elapsedMinutes <= ts.MaxMinutes
            );

            return matchingScore?.Score ?? 0;
        }

        public async Task<object> GetMediaDebugInfoAsync(Guid incidentId)
        {
            try
            {
                // Get all media for this incident using raw query
                var allMediaForIncident = await _unitOfWork.GetRepository<ReportMedia>()
                    .GetListAsync(
                        predicate: m => m.ReferenceId == incidentId && m.ReferenceType == MediaReferenceType.SnakebiteIncident
                    );

                // Get all media with any reference to this ID (regardless of type)
                var allMediaWithThisId = await _unitOfWork.GetRepository<ReportMedia>()
                    .GetListAsync(
                        predicate: m => m.ReferenceId == incidentId
                    );

                // Get all media for SnakebiteIncident type
                var allSnakebiteMedia = await _unitOfWork.GetRepository<ReportMedia>()
                    .GetListAsync(
                        predicate: m => m.ReferenceType == MediaReferenceType.SnakebiteIncident
                    );

                return new
                {
                    IncidentId = incidentId,
                    MediaForThisIncident = allMediaForIncident.Select(m => new
                    {
                        m.Id,
                        m.ReferenceId,
                        m.ReferenceType,
                        m.Purpose,
                        m.MediaUrl,
                        m.RequiresAIProcessing,
                        m.IsProcessed
                    }).ToList(),
                    CountForThisIncident = allMediaForIncident.Count,
                    MediaWithThisIdAnyType = allMediaWithThisId.Select(m => new
                    {
                        m.Id,
                        m.ReferenceId,
                        m.ReferenceType
                    }).ToList(),
                    AllSnakebiteIncidentMedia = allSnakebiteMedia.Select(m => new
                    {
                        m.Id,
                        m.ReferenceId,
                        m.ReferenceType
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting media debug info for incident {IncidentId}: {Message}", incidentId, ex.Message);
                throw;
            }
        }

        public async Task<IdentifySnakeResponse> IdentifySnakeByAIAsync(Guid incidentId, Guid recognitionResultId)
        {
            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    // 1. Validate incident exists
                    var incident = await _unitOfWork.GetRepository<SnakebiteIncident>()
                        .FirstOrDefaultAsync(predicate: i => i.Id == incidentId);

                    if (incident == null)
                    {
                        _logger.LogWarning("Snakebite incident not found: {IncidentId}", incidentId);
                        throw new NotFoundException("Snakebite incident not found.");
                    }

                    // 2. Validate recognition result exists and belongs to this incident
                    var recognitionResult = await _unitOfWork.GetRepository<SnakeAIRecognitionResult>()
                        .FirstOrDefaultAsync(
                            predicate: r => r.Id == recognitionResultId,
                            asNoTracking: false,
                            cancellationToken: default
                        );

                    if (recognitionResult == null)
                    {
                        _logger.LogWarning("Recognition result not found: {ResultId}", recognitionResultId);
                        throw new NotFoundException("Recognition result not found.");
                    }

                    // Explicitly load related entities
                    await _unitOfWork.Context.Entry(recognitionResult)
                        .Reference(r => r.ReportMedia)
                        .LoadAsync();

                    if (recognitionResult.DetectedSpeciesId.HasValue)
                    {
                        await _unitOfWork.Context.Entry(recognitionResult)
                            .Reference(r => r.DetectedSpecies)
                            .LoadAsync();
                    }

                    // Verify the recognition result's media belongs to this incident
                    if (recognitionResult.ReportMedia == null ||
                        recognitionResult.ReportMedia.ReferenceId != incidentId ||
                        recognitionResult.ReportMedia.ReferenceType != MediaReferenceType.SnakebiteIncident)
                    {
                        _logger.LogWarning("Recognition result {ResultId} does not belong to incident {IncidentId}",
                            recognitionResultId, incidentId);
                        throw new BadRequestException("Recognition result does not belong to this incident.");
                    }

                    if (recognitionResult.DetectedSpeciesId == null || recognitionResult.DetectedSpecies == null)
                    {
                        _logger.LogWarning("Recognition result {ResultId} has no detected species", recognitionResultId);
                        throw new BadRequestException("No species detected in this recognition result.");
                    }

                    // 3. Update incident with identification
                    incident.IdentifiedSnakeSpeciesId = recognitionResult.DetectedSpeciesId.Value;
                    incident.IdentificationMethod = SnakeIdentificationMethod.AIDetection;
                    incident.AIRecognitionResultId = recognitionResultId;
                    incident.IdentifiedAt = DateTime.UtcNow;

                    _unitOfWork.GetRepository<SnakebiteIncident>().Update(incident);

                    _logger.LogInformation("Snake identified for incident {IncidentId}: Species {SpeciesId} via AI with confidence {Confidence}",
                        incidentId, recognitionResult.DetectedSpeciesId, recognitionResult.Confidence);

                    return new IdentifySnakeResponse
                    {
                        IncidentId = incidentId,
                        IdentifiedSnakeSpeciesId = recognitionResult.DetectedSpeciesId.Value,
                        IdentificationMethod = SnakeIdentificationMethod.AIDetection,
                        IdentifiedAt = incident.IdentifiedAt.Value,
                        Snake = new SnakeSpeciesResponse
                        {
                            Id = recognitionResult.DetectedSpecies.Id,
                            ScientificName = recognitionResult.DetectedSpecies.ScientificName,
                            CommonName = recognitionResult.DetectedSpecies.CommonName ?? string.Empty,
                            Slug = recognitionResult.DetectedSpecies.Slug,
                            ImageUrl = recognitionResult.DetectedSpecies.ImageUrl,
                            Description = recognitionResult.DetectedSpecies.Description ?? string.Empty,
                            IdentificationSummary = recognitionResult.DetectedSpecies.IdentificationSummary ?? string.Empty,
                            PrimaryVenomType = recognitionResult.DetectedSpecies.PrimaryVenomType,
                            RiskLevel = recognitionResult.DetectedSpecies.RiskLevel,
                            IsVenomous = recognitionResult.DetectedSpecies.IsVenomous,
                            IsActive = recognitionResult.DetectedSpecies.IsActive
                        },
                        AIRecognitionResultId = recognitionResultId,
                        AIConfidence = (float)recognitionResult.Confidence
                    };
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error identifying snake by AI for incident {IncidentId}: {Message}", incidentId, ex.Message);
                throw;
            }
        }

        public async Task<IdentifySnakeResponse> IdentifySnakeByFilterAsync(Guid incidentId, IdentifyByFilterRequest request)
        {
            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    // 1. Validate incident exists
                    var incident = await _unitOfWork.GetRepository<SnakebiteIncident>()
                        .FirstOrDefaultAsync(predicate: i => i.Id == incidentId);

                    if (incident == null)
                    {
                        _logger.LogWarning("Snakebite incident not found: {IncidentId}", incidentId);
                        throw new NotFoundException("Snakebite incident not found.");
                    }

                    // 2. Validate selected snake species exists
                    var selectedSnake = await _unitOfWork.GetRepository<SnakeSpecies>()
                        .FirstOrDefaultAsync(
                            predicate: s => s.Id == request.SelectedSnakeSpeciesId && s.IsActive
                        );

                    if (selectedSnake == null)
                    {
                        _logger.LogWarning("Selected snake species not found or inactive: {SpeciesId}", request.SelectedSnakeSpeciesId);
                        throw new BadRequestException("Selected snake species is invalid or inactive.");
                    }

                    // 3. Validate selected options exist and are active
                    var selectedOptions = await _unitOfWork.GetRepository<FilterOption>()
                        .GetListAsync(
                            predicate: o => request.SelectedOptionIds.Contains(o.Id) && o.IsActive
                        );

                    if (selectedOptions.Count != request.SelectedOptionIds.Count)
                    {
                        throw new BadRequestException("Some selected options are invalid or inactive.");
                    }

                    // 4. Build FilterAnswerData (optimized - only store IDs)
                    var filterAnswerData = new FilterAnswerData
                    {
                        SelectedOptionIds = request.SelectedOptionIds,
                        SelectedSnakeSpeciesId = request.SelectedSnakeSpeciesId,
                        MatchScore = request.MatchScore,
                        MatchPercentage = request.MatchPercentage,
                        SelectedAt = DateTime.UtcNow
                    };

                    // 5. Update incident with identification
                    incident.IdentifiedSnakeSpeciesId = request.SelectedSnakeSpeciesId;
                    incident.IdentificationMethod = SnakeIdentificationMethod.FilterQuestions;
                    incident.FilterAnswers = filterAnswerData;
                    incident.IdentifiedAt = DateTime.UtcNow;

                    _unitOfWork.GetRepository<SnakebiteIncident>().Update(incident);

                    _logger.LogInformation(
                        "Snake identified for incident {IncidentId}: Species {SpeciesId} via filter questions with {MatchScore}/{TotalAnswers} matches ({MatchPercentage}%)",
                        incidentId,
                        request.SelectedSnakeSpeciesId,
                        request.MatchScore,
                        request.SelectedOptionIds.Count,
                        request.MatchPercentage
                    );

                    return new IdentifySnakeResponse
                    {
                        IncidentId = incidentId,
                        IdentifiedSnakeSpeciesId = selectedSnake.Id,
                        IdentificationMethod = SnakeIdentificationMethod.FilterQuestions,
                        IdentifiedAt = incident.IdentifiedAt.Value,
                        Snake = new SnakeSpeciesResponse
                        {
                            Id = selectedSnake.Id,
                            ScientificName = selectedSnake.ScientificName,
                            CommonName = selectedSnake.CommonName ?? string.Empty,
                            Slug = selectedSnake.Slug,
                            ImageUrl = selectedSnake.ImageUrl,
                            Description = selectedSnake.Description ?? string.Empty,
                            IdentificationSummary = selectedSnake.IdentificationSummary ?? string.Empty,
                            PrimaryVenomType = selectedSnake.PrimaryVenomType,
                            RiskLevel = selectedSnake.RiskLevel,
                            IsVenomous = selectedSnake.IsVenomous,
                            IsActive = selectedSnake.IsActive
                        },
                        MatchedSnakes = new List<string>
                        {
                            $"{selectedSnake.CommonName ?? selectedSnake.ScientificName} ({request.MatchScore}/{request.SelectedOptionIds.Count} matches - {request.MatchPercentage:F1}%)"
                        }
                    };
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error identifying snake by filter for incident {IncidentId}: {Message}", incidentId, ex.Message);
                throw;
            }
        }

        public Task<PagedData<ListSnakebiteIncidentResponse>> GetUserIncidentsAsync(Guid userId, SnakebiteIncidentStatus? status, int page, int pageSize)
        {
            try
            {
                var repo = _unitOfWork.GetRepository<SnakebiteIncident>();
                var userIncidents = repo.GetPagingListAsync<ListSnakebiteIncidentResponse>(
                    predicate: i => i.UserId == userId &&
                                    (!status.HasValue || i.Status == status.Value),
                    include: q => q.Include(i => i.Missions),
                    page: page,
                    size: pageSize,
                    orderBy: q => q.OrderByDescending(i => i.CreatedAt),
                    selector: i => i.Adapt<ListSnakebiteIncidentResponse>()
                );
                return userIncidents;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user incidents for user {UserId}: {Message}", userId, ex.Message);
                throw;
            }
        }

        public Task<PagedData<OperatorIncidentSummaryResponse>> GetActiveIncidentsAsync(
            IEnumerable<SnakebiteIncidentStatus>? statuses,
            DateTimeOffset? since,
            DateTimeOffset? until,
            int page,
            int pageSize)
        {
            try
            {
                var defaultStatuses = new[]
                {
                    SnakebiteIncidentStatus.Pending,
                    SnakebiteIncidentStatus.Verified,
                    SnakebiteIncidentStatus.Assigned,
                };

                var effectiveStatuses = (statuses != null && statuses.Any())
                    ? statuses
                    : defaultStatuses;

                var repo = _unitOfWork.GetRepository<SnakebiteIncident>();
                return repo.GetPagingListAsync<OperatorIncidentSummaryResponse>(
                    predicate: i =>
                        effectiveStatuses.Contains(i.Status) &&
                        (!since.HasValue || i.CreatedAt >= since.Value.UtcDateTime) &&
                        (!until.HasValue || i.CreatedAt <= until.Value.UtcDateTime),
                    include: q => q.Include(i => i.Missions),
                    orderBy: q => q.OrderByDescending(i => i.CreatedAt),
                    page: page,
                    size: pageSize,
                    selector: i => i.Adapt<OperatorIncidentSummaryResponse>());
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active incidents: {Message}", ex.Message);
                throw;
            }
        }

        public Task<PagedData<OperatorIncidentSummaryResponse>> GetAdminIncidentsAsync(
            IEnumerable<SnakebiteIncidentStatus>? statuses,
            DateTimeOffset? since,
            DateTimeOffset? until,
            int page,
            int pageSize)
        {
            try
            {
                var hasStatusFilter = statuses != null && statuses.Any();
                var repo = _unitOfWork.GetRepository<SnakebiteIncident>();

                return repo.GetPagingListAsync<OperatorIncidentSummaryResponse>(
                    predicate: i =>
                        (!hasStatusFilter || statuses!.Contains(i.Status)) &&
                        (!since.HasValue || i.CreatedAt >= since.Value.UtcDateTime) &&
                        (!until.HasValue || i.CreatedAt <= until.Value.UtcDateTime),
                    include: q => q.Include(i => i.Missions),
                    orderBy: q => q.OrderByDescending(i => i.CreatedAt),
                    page: page,
                    size: pageSize,
                    selector: i => i.Adapt<OperatorIncidentSummaryResponse>());
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error retrieving admin incidents: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<List<DispatchRequestResponse>> GetDispatchRequestsAsync(Guid incidentId)
        {
            try
            {
                var incident = await _unitOfWork.GetRepository<SnakebiteIncident>().FirstOrDefaultAsync(
                    predicate: i => i.Id == incidentId);

                if (incident == null)
                {
                    throw new NotFoundException($"Snakebite incident with ID {incidentId} not found.");
                }

                var repo = _unitOfWork.GetRepository<RescuerRequest>();
                var incidentRequests = await repo.GetListAsync(
                    predicate: r => r.IncidentId == incidentId,
                    include: q => q.Include(r => r.Rescuer)
                                    .ThenInclude(rescuer => rescuer.Account),
                    orderBy: q => q.OrderByDescending(r => r.CreatedAt)
                );

                return incidentRequests.Select(r => new DispatchRequestResponse
                {
                    RequestId = r.Id,
                    RescuerId = r.RescuerId,
                    RescuerName = r.Rescuer.Account.FullName,
                    RescuerPhone = r.Rescuer.Account.PhoneNumber ?? string.Empty,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt,
                    ResponseAt = r.ResponseAt,
                    DeclineReason = r.DeclineReason
                }).ToList();

            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error retrieving dispatch requests for incident {IncidentId}: {Message}", incidentId, ex.Message);
                throw;
            }
        }
    }
}
