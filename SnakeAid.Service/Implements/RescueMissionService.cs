using System;
using System.Collections.Generic;
using System.Linq;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Requests.RescueMission;
using SnakeAid.Core.Responses.Media;
using SnakeAid.Core.Responses.RescueMission;
using SnakeAid.Core.Responses.SnakeSpecies;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Extensions;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Service.Implements
{
    public class RescueMissionService : IRescueMissionService
    {
        private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
        private readonly ILogger<RescueMissionService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IRescueRequestSessionService _sessionService;

        // Default price for rescue mission (có thể lấy từ SystemSetting sau)
        private const decimal DEFAULT_RESCUE_PRICE = 500000m;
        private const decimal PRICE_PER_KM_DEFAULT = 5000m; // VND per km (fallback if config missing)


        private readonly IMissionNotificationService _notificationService;

        public RescueMissionService(
            IUnitOfWork<SnakeAidDbContext> unitOfWork,
            ILogger<RescueMissionService> logger,
            IConfiguration configuration,
            IRescueRequestSessionService sessionService,
            IMissionNotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _configuration = configuration;
            _sessionService = sessionService;
            _notificationService = notificationService;
        }

        /// <summary>
        /// Tạo mission khi rescuer accept request
        /// </summary>
        public async Task<RescueMission> CreateMissionAsync(Guid incidentId, Guid rescuerId, decimal price)
        {
            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    // Verify incident exists and is in correct state
                    var incident = await _unitOfWork.GetRepository<SnakebiteIncident>().FirstOrDefaultAsync(
                        predicate: i => i.Id == incidentId
                    );

                    if (incident == null)
                    {
                        throw new NotFoundException("Incident not found.");
                    }

                    if (incident.Status != SnakebiteIncidentStatus.Pending)
                    {
                        throw new BadRequestException($"Cannot create mission for incident with status: {incident.Status}");
                    }

                    // Verify rescuer exists
                    var rescuer = await _unitOfWork.GetRepository<RescuerProfile>().FirstOrDefaultAsync(
                        predicate: r => r.AccountId == rescuerId
                    );

                    if (rescuer == null)
                    {
                        throw new NotFoundException("Rescuer not found.");
                    }

                    // Check for active missions only (allow multiple missions per incident for retry scenarios)
                    var existingActiveMission = await _unitOfWork.GetRepository<RescueMission>().FirstOrDefaultAsync(
                        predicate: m => m.IncidentId == incidentId &&
                            (m.Status == RescueMissionStatus.Preparing ||
                             m.Status == RescueMissionStatus.EnRoute ||
                             m.Status == RescueMissionStatus.RescuerArrived)
                    );

                    if (existingActiveMission != null)
                    {
                        throw new BadRequestException($"Active mission {existingActiveMission.Id} already exists for this incident.");
                    }

                    // Create new mission
                    var mission = new RescueMission
                    {
                        Id = Guid.NewGuid(),
                        IncidentId = incidentId,
                        RescuerId = rescuerId,
                        Status = RescueMissionStatus.Preparing,
                        Price = price > 0 ? price : DEFAULT_RESCUE_PRICE,
                        CreatedAt = DateTime.UtcNow
                    };

                    // Update incident status
                    incident.Status = SnakebiteIncidentStatus.Assigned;
                    incident.AssignedRescuerId = rescuerId;
                    incident.AssignedAt = DateTime.UtcNow;

                    // Mark rescuer as unavailable (on mission)
                    rescuer.IsAvailable = false;
                    _unitOfWork.GetRepository<RescuerProfile>().Update(rescuer);

                    await _unitOfWork.GetRepository<RescueMission>().InsertAsync(mission);
                    _unitOfWork.GetRepository<SnakebiteIncident>().Update(incident);

                    _logger.LogInformation("Created mission {MissionId} for incident {IncidentId} with rescuer {RescuerId}",
                        mission.Id, incidentId, rescuerId);

                    return mission;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating mission for incident {IncidentId}: {Message}", incidentId, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Update mission status (e.g., EnRoute, Arrived)
        /// For completion, use CompleteMissionAsync instead
        /// </summary>
        public async Task UpdateMissionStatusAsync(Guid missionId, RescueMissionStatus status)
        {
            // Don't allow direct completion via this method - use CompleteMissionAsync
            // if (status == RescueMissionStatus.MissionCompleted)
            // {
            //     throw new BadRequestException("Use CompleteMissionAsync to complete mission with evidence photos.");
            // }

            RescueMission mission;

            try
            {
                // Step 1: Update database in transaction
                mission = await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var missionEntity = await _unitOfWork.GetRepository<RescueMission>().FirstOrDefaultAsync(
                        predicate: m => m.Id == missionId
                    );

                    if (missionEntity == null)
                    {
                        throw new NotFoundException("Mission not found.");
                    }

                    // Validate state transition
                    if (!IsValidStatusTransition(missionEntity.Status, status))
                    {
                        throw new BadRequestException($"Cannot transition from {missionEntity.Status} to {status}");
                    }

                    missionEntity.Status = status;
                    missionEntity.UpdatedAt = DateTime.UtcNow;

                    // Set timestamps based on status
                    switch (status)
                    {
                        case RescueMissionStatus.EnRoute:
                            missionEntity.StartedAt = DateTime.UtcNow;
                            break;
                        case RescueMissionStatus.RescuerArrived:
                            missionEntity.ArrivedAt = DateTime.UtcNow;
                            break;
                    }

                    _unitOfWork.GetRepository<RescueMission>().Update(missionEntity);

                    _logger.LogInformation("Updated mission {MissionId} status to {Status}", missionId, status);

                    return missionEntity;
                });

                // Step 2: Send notifications AFTER transaction committed
                if (status == RescueMissionStatus.RescuerArrived)
                {
                    await _notificationService.NotifyRescuerArrivedAsync(mission.IncidentId);
                }
                else if (status == RescueMissionStatus.EnRoute)
                {
                    await _notificationService.NotifyMissionStartedAsync(mission.IncidentId, new { status = status.ToString() });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating mission {MissionId} status: {Message}", missionId, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Complete mission with evidence photos validation
        /// </summary>
        public async Task CompleteMissionAsync(Guid missionId, List<Guid> evidenceMediaIds, string? completionNotes)
        {
            RescueMission mission;

            try
            {
                // Step 1: Update database in transaction
                mission = await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    // 1. Get mission
                    var missionEntity = await _unitOfWork.GetRepository<RescueMission>().FirstOrDefaultAsync(
                        predicate: m => m.Id == missionId
                    );

                    if (missionEntity == null)
                    {
                        throw new NotFoundException("Mission not found.");
                    }

                    // 2. Validate status transition
                    if (!IsValidStatusTransition(missionEntity.Status, RescueMissionStatus.MissionCompleted))
                    {
                        throw new BadRequestException($"Cannot complete mission with status: {missionEntity.Status}. Mission must be in RescuerArrived status.");
                    }

                    // 3. Validate evidence media
                    if (evidenceMediaIds == null || !evidenceMediaIds.Any())
                    {
                        throw new BadRequestException("At least one evidence photo is required to complete the mission.");
                    }

                    var reportMediaRepo = _unitOfWork.GetRepository<ReportMedia>();
                    var evidenceMedia = await reportMediaRepo.GetListAsync(
                        predicate: m => evidenceMediaIds.Contains(m.Id),
                        asNoTracking: false
                    );

                    if (evidenceMedia.Count != evidenceMediaIds.Count)
                    {
                        throw new BadRequestException("One or more evidence media not found.");
                    }

                    // 4. Validate all media belong to this mission and are Evidence type
                    var invalidMedia = evidenceMedia.Where(m =>
                        m.ReferenceId != missionId ||
                        m.ReferenceType != MediaReferenceType.RescueMission ||
                        m.Purpose != MediaPurpose.Evidence
                    ).ToList();

                    if (invalidMedia.Any())
                    {
                        var invalidIds = string.Join(", ", invalidMedia.Select(m => m.Id));
                        throw new BadRequestException($"Invalid evidence media: {invalidIds}. All media must belong to this mission and have Purpose=Evidence.");
                    }

                    // 6. Update mission
                    missionEntity.Status = RescueMissionStatus.MissionCompleted;
                    missionEntity.CompletedAt = DateTime.UtcNow;
                    missionEntity.UpdatedAt = DateTime.UtcNow;
                    missionEntity.Notes = completionNotes;

                    // 7. Update incident status to Finished
                    var incident = await _unitOfWork.GetRepository<SnakebiteIncident>().FirstOrDefaultAsync(
                        predicate: i => i.Id == missionEntity.IncidentId
                    );

                    if (incident != null)
                    {
                        incident.Status = SnakebiteIncidentStatus.Finished;
                        _unitOfWork.GetRepository<SnakebiteIncident>().Update(incident);
                    }

                    _unitOfWork.GetRepository<RescueMission>().Update(missionEntity);

                    _logger.LogInformation(
                        "Completed mission {MissionId} with {EvidenceCount} evidence photos. Notes: {Notes}",
                        missionId, evidenceMediaIds.Count, completionNotes ?? "None");

                    return missionEntity;
                });

                // Step 2: Send notification AFTER transaction committed
                await _notificationService.NotifyMissionCompletedAsync(mission.IncidentId, new { missionId = missionId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing mission {MissionId}: {Message}", missionId, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// User cancel mission: Set status to Cancelled, no new session
        /// Only allowed during Preparing phase (before rescuer starts moving)
        /// </summary>
        public async Task UserCancelMissionAsync(Guid missionId, string reason)
        {
            RescueMission mission;

            try
            {
                // Step 1: Update database in transaction
                mission = await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var missionEntity = await _unitOfWork.GetRepository<RescueMission>().FirstOrDefaultAsync(
                        predicate: m => m.Id == missionId,
                        include: q => q.Include(m => m.Incident)
                    );

                    if (missionEntity == null)
                    {
                        throw new NotFoundException("Mission not found.");
                    }

                    // Validate status transition using centralized method
                    if (!IsValidStatusTransition(missionEntity.Status, RescueMissionStatus.Cancelled))
                    {
                        throw new BadRequestException($"User cannot cancel mission with status: {missionEntity.Status}. Only allowed during Preparing phase.");
                    }

                    missionEntity.Status = RescueMissionStatus.Cancelled;
                    missionEntity.CancellationReason = reason;
                    missionEntity.UpdatedAt = DateTime.UtcNow;

                    // Set incident to Cancelled (user doesn't want rescue anymore)
                    var incident = missionEntity.Incident;
                    incident.Status = SnakebiteIncidentStatus.Cancelled;
                    incident.AssignedRescuerId = null;
                    incident.AssignedAt = null;

                    _unitOfWork.GetRepository<RescueMission>().Update(missionEntity);
                    _unitOfWork.GetRepository<SnakebiteIncident>().Update(incident);

                    _logger.LogInformation("User cancelled mission {MissionId} with reason: {Reason}", missionId, reason);

                    return missionEntity;
                });

                // Step 2: Send notification AFTER transaction committed
                await _notificationService.NotifyMissionCancelledAsync(mission.IncidentId, reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in user cancelling mission {MissionId}: {Message}", missionId, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Rescuer abort mission: Set status to MissionAborted, create new session with increased radius
        /// Allowed during Preparing or EnRoute phases (not allowed after RescuerArrived)
        /// </summary>
        public async Task RescuerAbortMissionAsync(Guid missionId, string reason)
        {
            Guid incidentId;

            try
            {
                // Step 1: Abort mission in transaction
                incidentId = await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    // Query mission WITHOUT Include to avoid navigation property tracking issues
                    var mission = await _unitOfWork.GetRepository<RescueMission>().FirstOrDefaultAsync(
                        predicate: m => m.Id == missionId
                    );

                    if (mission == null)
                    {
                        throw new NotFoundException("Mission not found.");
                    }

                    // Validate status transition using centralized method
                    if (!IsValidStatusTransition(mission.Status, RescueMissionStatus.MissionAborted))
                    {
                        throw new BadRequestException($"Cannot abort mission with status: {mission.Status}. Only allowed during Preparing or EnRoute phases.");
                    }

                    // Query incident SEPARATELY to ensure proper EF tracking
                    // Using navigation property (mission.Incident) causes Update() to not mark entity as Modified
                    var incident = await _unitOfWork.GetRepository<SnakebiteIncident>().FirstOrDefaultAsync(
                        predicate: i => i.Id == mission.IncidentId
                    );

                    if (incident == null)
                    {
                        throw new NotFoundException("Incident not found.");
                    }

                    _logger.LogInformation("Aborting mission {MissionId}: Current incident status before update: {Status}",
                        missionId, incident.Status);

                    // Update mission
                    mission.Status = RescueMissionStatus.MissionAborted;
                    mission.CancellationReason = reason;
                    mission.UpdatedAt = DateTime.UtcNow;

                    // Reset incident to Pending for retry with increased radius
                    incident.Status = SnakebiteIncidentStatus.Pending;
                    incident.AssignedRescuerId = null;
                    incident.AssignedAt = null;

                    _unitOfWork.GetRepository<RescueMission>().Update(mission);
                    _unitOfWork.GetRepository<SnakebiteIncident>().Update(incident);

                    _logger.LogInformation("Updated incident {IncidentId} to Pending status in transaction", incident.Id);

                    return mission.IncidentId;
                });

                _unitOfWork.ClearChangeTracker();

                _logger.LogInformation("Transaction committed and change tracker cleared for incident {IncidentId}. Tracked entities after clear: {TrackedCount}",
                    incidentId, _unitOfWork.Context.ChangeTracker.Entries().Count());

                // PUSH NOTIFICATION: Notify Member about rescuer abort AFTER transaction committed
                await _notificationService.NotifyMissionCancelledAsync(incidentId, reason);

                // Step 2: Create new session AFTER transaction committed
                try
                {
                    await _sessionService.HandleMissionAbortAsync(incidentId);
                    _logger.LogInformation("Created new rescue session after rescuer abort for incident {IncidentId}", incidentId);
                }
                catch (Exception sessionEx)
                {
                    _logger.LogError(sessionEx, "Failed to create new session after rescuer abort for incident {IncidentId}: {Message}",
                        incidentId, sessionEx.Message);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in rescuer aborting mission {MissionId}: {Message}", missionId, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Validate mission status transition
        /// </summary>
        private bool IsValidStatusTransition(RescueMissionStatus current, RescueMissionStatus next)
        {
            return (current, next) switch
            {
                // Normal flow
                (RescueMissionStatus.Preparing, RescueMissionStatus.EnRoute) => true,
                (RescueMissionStatus.EnRoute, RescueMissionStatus.RescuerArrived) => true,
                (RescueMissionStatus.RescuerArrived, RescueMissionStatus.MissionCompleted) => true,
                (RescueMissionStatus.RescuerArrived, RescueMissionStatus.MissionUncompleted) => true,

                // User cancellation: Only during Preparing phase
                (RescueMissionStatus.Preparing, RescueMissionStatus.Cancelled) => true,

                // Rescuer abort: Only during Preparing or EnRoute
                (RescueMissionStatus.Preparing, RescueMissionStatus.MissionAborted) => true,
                (RescueMissionStatus.EnRoute, RescueMissionStatus.MissionAborted) => true,

                _ => false
            };
        }

        public async Task<RescueMission> GetMissionByIdAsync(Guid missionId)
        {
            try
            {
                var repo = _unitOfWork.GetRepository<RescueMission>();
                var mission = await repo.FirstOrDefaultAsync(
                    predicate: m => m.Id == missionId,
                    include: q => q.Include(mission => mission.Incident)
                ) ?? throw new NotFoundException($"Mission {missionId} not found.");
                return mission;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving mission {MissionId}: {Message}", missionId, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Get detailed mission information with all related entities
        /// Includes incident media with AI recognition results for rescuer to see snake photos
        /// </summary>
        public async Task<DetailRescueMissionResponse> GetMissionDetailAsync(Guid missionId)
        {
            try
            {
                var mission = await _unitOfWork.GetRepository<RescueMission>().FirstOrDefaultAsync(
                    predicate: m => m.Id == missionId,
                    include: q => q
                        .Include(m => m.Incident)
                            .ThenInclude(i => i.User)
                                .ThenInclude(u => u.Account)
                        .Include(m => m.Incident)
                            .ThenInclude(i => i.IdentifiedSnakeSpecies)
                        .Include(m => m.Incident)
                            .ThenInclude(i => i.AIRecognitionResult)
                        .Include(m => m.Rescuer)
                            .ThenInclude(r => r.Account)
                );

                if (mission == null)
                {
                    throw new NotFoundException($"Mission {missionId} not found.");
                }

                await mission.Incident.AttachReportMediaAsync(_unitOfWork, MediaReferenceType.SnakebiteIncident);

                // DEBUG: Log media and AI results
                _logger.LogInformation("Mission {MissionId}: Loaded {MediaCount} media items",
                    missionId, mission.Incident.Media?.Count ?? 0);

                var response = mission.Adapt<DetailRescueMissionResponse>();

                // Manually map identified snake and identification context if available
                if (mission.Incident.IdentifiedSnakeSpecies != null && response.Incident != null)
                {
                    response.Incident.IdentifiedSnake = mission.Incident.IdentifiedSnakeSpecies.Adapt<SnakeSpeciesResponse>();

                    response.Incident.IdentificationContext = new Core.Responses.FirstAid.SnakeIdentificationContext
                    {
                        Method = mission.Incident.IdentificationMethod,
                        IdentifiedAt = mission.Incident.IdentifiedAt ?? DateTime.UtcNow
                    };

                    // Add AI confidence if applicable
                    if (mission.Incident.IdentificationMethod == SnakeIdentificationMethod.AIDetection
                        && mission.Incident.AIRecognitionResult != null)
                    {
                        response.Incident.IdentificationContext.AIConfidence = (float)mission.Incident.AIRecognitionResult.Confidence;
                    }
                }

                // // Manual map Media to ensure DetectedSpecies are properly mapped
                // // Mapster có thể không handle đúng complex LINQ trong nested mapping
                // if (mission.Incident.Media != null && mission.Incident.Media.Any() && response.Incident != null)
                // {
                //     response.Incident.Media = mission.Incident.Media.Adapt<List<SnakeAIDetectMediaResponse>>();
                // }

                _logger.LogInformation("Mapped response: Incident Media Count = {MediaCount}",
                    response.Incident?.Media?.Count ?? 0);
                // DEBUG: Log mapped media DetectedSpecies
                foreach (var mappedMedia in response.Incident?.Media ?? Enumerable.Empty<SnakeAIDetectMediaResponse>())
                {
                    _logger.LogInformation(
                        "Mapped Media {MediaId}: DetectedSpecies Count = {SpeciesCount}",
                        mappedMedia.Id,
                        mappedMedia.DetectedSpecies?.Count ?? 0);

                    foreach (var species in mappedMedia.DetectedSpecies ?? Enumerable.Empty<SnakeSpeciesResponse>())
                    {
                        _logger.LogInformation("  - Species: {SpeciesId} {CommonName}", species.Id, species.CommonName);
                    }
                }
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving mission detail {MissionId}: {Message}", missionId, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Get mission detail with distance calculation
        /// </summary>
        public async Task<DetailRescueMissionResponse> GetMissionDetailAsync(Guid missionId, double? rescuerLat, double? rescuerLng)
        {
            try
            {
                var response = await GetMissionDetailAsync(missionId);

                // Calculate distance if rescuer location is provided
                if (rescuerLat.HasValue && rescuerLng.HasValue)
                {
                    var incidentLat = response.Incident.LocationCoordinates.Latitude;
                    var incidentLng = response.Incident.LocationCoordinates.Longitude;
                    response.DistanceKm = CalculateDistance(rescuerLat.Value, rescuerLng.Value, incidentLat, incidentLng);
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving mission detail with distance {MissionId}: {Message}", missionId, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Calculate distance between two GPS coordinates using Haversine formula
        /// </summary>
        private double CalculateDistance(double lat1, double lng1, double lat2, double lng2)
        {
            const double earthRadiusKm = 6371.0;

            var dLat = ToRadians(lat2 - lat1);
            var dLng = ToRadians(lng2 - lng1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLng / 2) * Math.Sin(dLng / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var distance = earthRadiusKm * c;

            return Math.Round(distance, 2);
        }

        private double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }

        public async Task<HospitalTransferPricingResponse> ReportHospitalTransferAsync(
            Guid missionId,
            Guid rescuerId,
            ReportHospitalTransferRequest request)
        {
            try
            {
                // Step 1: Validate mission and rescuer
                var mission = await _unitOfWork.GetRepository<RescueMission>()
                    .FirstOrDefaultAsync(
                        predicate: m => m.Id == missionId && m.RescuerId == rescuerId,
                        include: q => q.Include(m => m.Incident)
                    );

                if (mission == null)
                    throw new NotFoundException("Mission not found or you are not assigned to this mission");

                if (mission.Status != RescueMissionStatus.RescuerArrived)
                    throw new BadRequestException("Can only report hospital transfer after arriving at incident location");

                // Step 2: Get hospital info
                var hospital = await _unitOfWork.GetRepository<TreatmentFacility>()
                    .FirstOrDefaultAsync(predicate: h => h.Id == request.HospitalId && h.IsActive);

                if (hospital == null)
                    throw new NotFoundException("Hospital not found or inactive");

                // Step 3: Get pricing config (from SystemSettings or Configuration)
                var pricePerKm = _configuration.GetValue<decimal>("Pricing:HospitalTransferPerKm", PRICE_PER_KM_DEFAULT);

                // Step 4: Calculate price (simple multiplication)
                var transferPrice = request.DistanceToHospitalKm * pricePerKm;
                var totalPrice = mission.Price + transferPrice;

                // Step 5: Update mission with hospital transfer info
                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    mission.RequiresHospitalization = true;
                    mission.HospitalId = request.HospitalId;
                    mission.DistanceToHospitalKm = request.DistanceToHospitalKm;
                    mission.HospitalTransferPrice = transferPrice;

                    if (!string.IsNullOrWhiteSpace(request.Notes))
                    {
                        mission.Notes = string.IsNullOrWhiteSpace(mission.Notes)
                            ? $"[Hospital Transfer] {request.Notes}"
                            : $"{mission.Notes}\n[Hospital Transfer] {request.Notes}";
                    }

                    _unitOfWork.GetRepository<RescueMission>().Update(mission);
                    await _unitOfWork.CommitAsync();

                    _logger.LogInformation(
                        "✅ Hospital transfer reported - MissionId: {MissionId}, HospitalId: {HospitalId}, " +
                        "Distance: {DistanceKm}km, TransferPrice: {TransferPrice} VND, TotalPrice: {TotalPrice} VND",
                        missionId, request.HospitalId, request.DistanceToHospitalKm, transferPrice, totalPrice);

                    return new HospitalTransferPricingResponse
                    {
                        HospitalId = hospital.Id,
                        HospitalName = hospital.Name,
                        DistanceKm = request.DistanceToHospitalKm,
                        PricePerKm = pricePerKm,
                        HospitalTransferPrice = transferPrice,
                        BaseMissionPrice = mission.Price,
                        TotalPrice = totalPrice,
                        CalculatedAt = DateTime.UtcNow
                    };
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reporting hospital transfer for mission {MissionId}: {Message}",
                    missionId, ex.Message);
                throw;
            }
        }
    }
}