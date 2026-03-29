using Mapster;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Responses.Auth;
using SnakeAid.Core.Responses.RescuerProfile;
using SnakeAid.Core.Utils;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Extensions;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Service.Implements
{
    public class OperatorSnapshotService : IOperatorSnapshotService
    {
        private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
        private readonly ILogger<OperatorSnapshotService> _logger;

        public OperatorSnapshotService(
            IUnitOfWork<SnakeAidDbContext> unitOfWork,
            ILogger<OperatorSnapshotService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<List<BriefRescuerProfileResponse>> GetOnlineRescuersAsync()
        {
            var onlineRescuers = await _unitOfWork.GetRepository<RescuerProfile>().GetListAsync(
                predicate: r => r.IsOnline,
                include: q => q.Include(r => r.Account),
                orderBy: q => q.OrderByDescending(r => r.UpdatedAt));

            var response = onlineRescuers.Adapt<List<BriefRescuerProfileResponse>>();

            _logger.LogInformation("Retrieved {Count} online rescuer(s).", response.Count);

            return response;
        }

        public async Task<OnDutyRescuerSnapshotResponse> GetOnDutyRescuersAsync(
            DateOnly? date,
            Guid? incidentId,
            Guid? catchingRequestId,
            bool onlyAvailable,
            double? maxDistanceKm)
        {
            if (incidentId.HasValue && catchingRequestId.HasValue)
            {
                throw new ArgumentException("Only one of incidentId or catchingRequestId may be provided.");
            }

            if (incidentId.HasValue)
            {
                return await GetOnDutyRescuersForIncidentAsync(date, incidentId.Value, onlyAvailable, maxDistanceKm);
            }

            if (catchingRequestId.HasValue)
            {
                return await GetOnDutyRescuersForCatchingRequestAsync(date, catchingRequestId.Value, onlyAvailable, maxDistanceKm);
            }

            var nowLocal = AppTime.NowLocal;
            return await BuildSnapshotAsync(
                date ?? DateOnly.FromDateTime(nowLocal),
                nowLocal,
                null,
                new HashSet<Guid>(),
                onlyAvailable,
                maxDistanceKm);
        }

        public async Task<OnDutyRescuerSnapshotResponse> GetOnDutyRescuersForIncidentAsync(
            DateOnly? date,
            Guid incidentId,
            bool onlyAvailable,
            double? maxDistanceKm)
        {
            var nowLocal = AppTime.NowLocal;
            var targetDate = date ?? DateOnly.FromDateTime(nowLocal);

            var incident = await _unitOfWork.GetRepository<SnakebiteIncident>().FirstOrDefaultAsync(
                predicate: i => i.Id == incidentId);

            if (incident == null)
            {
                throw new NotFoundException("Incident not found.");
            }

            var excludedRescuerIds = new HashSet<Guid>();

            var declinedRescuerIds = await _unitOfWork.GetRepository<RescuerRequest>().CreateBaseQuery(asNoTracking: true)
                .Where(r => r.IncidentId == incidentId && r.Status == RescueRequestStatus.Declined)
                .Select(r => r.RescuerId)
                .Distinct()
                .ToListAsync();

            var abortedRescuerIds = await _unitOfWork.GetRepository<RescueMission>().CreateBaseQuery(asNoTracking: true)
                .Where(m => m.IncidentId == incidentId && m.Status == RescueMissionStatus.MissionAborted)
                .Select(m => m.RescuerId)
                .Distinct()
                .ToListAsync();

            excludedRescuerIds = declinedRescuerIds
                .Concat(abortedRescuerIds)
                .ToHashSet();

            return await BuildSnapshotAsync(
                targetDate,
                nowLocal,
                incident.LocationCoordinates,
                excludedRescuerIds,
                onlyAvailable,
                maxDistanceKm);
        }

        public async Task<OnDutyRescuerSnapshotResponse> GetOnDutyRescuersForCatchingRequestAsync(
            DateOnly? date,
            Guid catchingRequestId,
            bool onlyAvailable,
            double? maxDistanceKm)
        {
            var nowLocal = AppTime.NowLocal;
            var targetDate = date ?? DateOnly.FromDateTime(nowLocal);

            var catchingRequest = await _unitOfWork.GetRepository<SnakeCatchingRequest>().FirstOrDefaultAsync(
                predicate: r => r.Id == catchingRequestId);

            if (catchingRequest == null)
            {
                throw new NotFoundException("Catching request not found.");
            }

            // Exclude rescuer IDs that already declined or aborted this catching request
            var excludedRescuerIds = new HashSet<Guid>();

            if (catchingRequest.AssignedRescuerId.HasValue)
            {
                excludedRescuerIds.Add(catchingRequest.AssignedRescuerId.Value);
            }

            var abortedRescuerIds = await _unitOfWork.GetRepository<SnakeCatchingMission>().CreateBaseQuery(asNoTracking: true)
                .Where(m => m.SnakeCatchingRequestId == catchingRequestId &&
                            (m.Status == CatchingMissionStatus.MissionAborted || m.Status == CatchingMissionStatus.Cancelled))
                .Select(m => m.RescuerId)
                .Distinct()
                .ToListAsync();

            excludedRescuerIds.UnionWith(abortedRescuerIds);

            return await BuildSnapshotAsync(
                targetDate,
                nowLocal,
                catchingRequest.LocationCoordinates,
                excludedRescuerIds,
                onlyAvailable,
                maxDistanceKm);
        }

        private async Task<OnDutyRescuerSnapshotResponse> BuildSnapshotAsync(
            DateOnly targetDate,
            DateTime nowLocal,
            NetTopologySuite.Geometries.Point? locationPoint,
            HashSet<Guid> excludedRescuerIds,
            bool onlyAvailable,
            double? maxDistanceKm)
        {
            var assignments = await _unitOfWork.GetRepository<ShiftAssignment>().GetListAsync(
                predicate: a => (a.Status == ShiftAssignmentStatus.Scheduled || a.Status == ShiftAssignmentStatus.Active)
                                && a.ShiftStartLocal <= nowLocal
                                && a.ShiftEndLocal >= nowLocal,
                include: q => q
                    .Include(a => a.Shift)
                    .Include(a => a.Rescuer)
                    .ThenInclude(r => r.Account));

            var rescuerItems = assignments
                .Where(a => a.Rescuer != null && a.Shift != null)
                .GroupBy(a => a.RescuerId)
                .Select(g => SelectBestAssignment(g.ToList(), nowLocal))
                .Where(a => a != null)
                .Where(a => !excludedRescuerIds.Contains(a!.RescuerId))
                .Select(a => BuildRescuerItem(a!, nowLocal, distanceKm: null))
                .Where(item => !onlyAvailable || (item.IsOnline && item.IsAvailable))
                .ToList();

            // If we have a location point, compute best available distances via database (PostGIS)
            if (locationPoint != null && rescuerItems.Any() && rescuerItems.Any(i => i.Latitude.HasValue && i.Longitude.HasValue))
            {
                var rescuerIds = rescuerItems.Select(i => i.RescuerId).ToList();
                var distanceResults = await _unitOfWork.GetRepository<RescuerProfile>()
                    .GetListAsync(
                        predicate: r => rescuerIds.Contains(r.AccountId) && r.LastLocation != null,
                        selector: r => new
                        {
                            Id = r.AccountId,
                            DistanceKm = EF.Functions.Distance(r.LastLocation!, locationPoint, true) / 1000
                        });

                var distanceMap = distanceResults.ToDictionary(x => x.Id, x => (double?)x.DistanceKm);

                foreach (var item in rescuerItems)
                {
                    if (distanceMap.TryGetValue(item.RescuerId, out var d))
                    {
                        item.DistanceKm = Math.Round(d ?? 0, 2);
                    }
                }
            }

            // Apply distance filter and final ordering
            rescuerItems = rescuerItems
                .Where(item => !maxDistanceKm.HasValue || (item.DistanceKm.HasValue && item.DistanceKm.Value <= maxDistanceKm.Value))
                .OrderBy(item => item.DistanceKm ?? double.MaxValue)
                .ThenByDescending(item => item.IsOnDutyNow)
                .ThenBy(item => item.ShiftStartTime)
                .ToList();

            _logger.LogInformation(
                "Snapshot on-duty rescuers built. Date={Date}, Count={Count}, OnlyAvailable={OnlyAvailable}, MaxDistanceKm={MaxDistanceKm}",
                targetDate,
                rescuerItems.Count,
                onlyAvailable,
                maxDistanceKm);

            return new OnDutyRescuerSnapshotResponse
            {
                ContextId = null,
                Date = targetDate,
                SnapshotAt = AppTime.UtcNow,
                Rescuers = rescuerItems
            };
        }


        private static ShiftAssignment? SelectBestAssignment(List<ShiftAssignment> assignments, DateTime nowLocal)
        {
            return assignments
                .OrderByDescending(a => a.Status == ShiftAssignmentStatus.Active)
                .ThenByDescending(a => a.IsOnDutyNow(nowLocal))
                .ThenBy(a => a.ShiftStartLocal)
                .FirstOrDefault();
        }

        private static OnDutyRescuerItemResponse BuildRescuerItem(ShiftAssignment assignment, DateTime nowLocal, double? distanceKm)
        {
            var rescuer = assignment.Rescuer;
            var isOnDutyNow = assignment.IsOnDutyNow(nowLocal);

            var latitude = rescuer.LastLocation?.Y;
            var longitude = rescuer.LastLocation?.X;

            return new OnDutyRescuerItemResponse
            {
                RescuerId = rescuer.AccountId,
                FullName = rescuer.Account?.FullName ?? string.Empty,
                PhoneNumber = rescuer.Account?.PhoneNumber,
                IsOnline = rescuer.IsOnline,
                IsAvailable = rescuer.IsAvailable,
                IsOnDutyNow = isOnDutyNow,
                AssignmentStatus = assignment.Status.ToString(),
                ShiftAssignmentId = assignment.Id,
                ShiftId = assignment.ShiftId,
                ShiftName = assignment.Shift.Name,
                ShiftStartTime = assignment.ShiftStartLocal.TimeOfDay,
                ShiftEndTime = assignment.ShiftEndLocal.TimeOfDay,
                ShiftDate = DateOnly.FromDateTime(assignment.ShiftStartLocal),
                Latitude = latitude,
                Longitude = longitude,
                LastLocationUpdate = rescuer.LastLocationUpdate,
                DistanceKm = distanceKm
            };
        }

        public async Task<List<BriefRescuerProfileResponse>> GetRescuerRegistryAsync()
        {
            var profiles = await _unitOfWork.GetRepository<RescuerProfile>().GetListAsync(
                include: q => q.Include(p => p.Account));

            return profiles.Select(p => p.Adapt<BriefRescuerProfileResponse>()).ToList();
        }

        public async Task<BriefRescuerProfileResponse?> GetRescuerByIdAsync(Guid rescuerId)
        {
            var profile = await _unitOfWork.GetRepository<RescuerProfile>().FirstOrDefaultAsync(
                predicate: p => p.AccountId == rescuerId,
                include: q => q.Include(p => p.Account));

            return profile?.Adapt<BriefRescuerProfileResponse>();
        }
    }
}
