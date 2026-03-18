using Mapster;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mapster;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Responses.Auth;
using SnakeAid.Core.Responses.RescuerProfile;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
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
            bool onlyAvailable,
            double? maxDistanceKm)
        {
            var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var nowUtc = DateTime.UtcNow;

            SnakebiteIncident? incident = null;
            var excludedRescuerIds = new HashSet<Guid>();
            if (incidentId.HasValue)
            {
                incident = await _unitOfWork.GetRepository<SnakebiteIncident>().FirstOrDefaultAsync(
                    predicate: i => i.Id == incidentId.Value);

                if (incident == null)
                {
                    throw new NotFoundException("Incident not found.");
                }

                var declinedRescuerIds = await _unitOfWork.GetRepository<RescuerRequest>().CreateBaseQuery(asNoTracking: true)
                    .Where(r => r.IncidentId == incidentId.Value && r.Status == RescueRequestStatus.Declined)
                    .Select(r => r.RescuerId)
                    .Distinct()
                    .ToListAsync();

                var abortedRescuerIds = await _unitOfWork.GetRepository<RescueMission>().CreateBaseQuery(asNoTracking: true)
                    .Where(m => m.IncidentId == incidentId.Value && m.Status == RescueMissionStatus.MissionAborted)
                    .Select(m => m.RescuerId)
                    .Distinct()
                    .ToListAsync();

                excludedRescuerIds = declinedRescuerIds
                    .Concat(abortedRescuerIds)
                    .ToHashSet();
            }

            var assignments = await _unitOfWork.GetRepository<ShiftAssignment>().GetListAsync(
                predicate: a => a.Date == targetDate
                                && (a.Status == ShiftAssignmentStatus.Scheduled || a.Status == ShiftAssignmentStatus.Active),
                include: q => q
                    .Include(a => a.Shift)
                    .Include(a => a.Rescuer)
                    .ThenInclude(r => r.Account));

            var rescuerItems = assignments
                .Where(a => a.Rescuer != null && a.Shift != null)
                .GroupBy(a => a.RescuerId)
                .Select(g => SelectBestAssignment(g.ToList(), nowUtc, targetDate))
                .Where(a => a != null)
                .Where(a => !excludedRescuerIds.Contains(a!.RescuerId))
                .Select(a => BuildRescuerItem(a!, incident, nowUtc, distanceKm: null))
                .Where(item => !onlyAvailable || (item.IsOnline && item.IsAvailable))
                .ToList();

            // If we have incident location, compute best available distances via database (PostGIS)
            if (incident != null && rescuerItems.Any() && rescuerItems.Any(i => i.Latitude.HasValue && i.Longitude.HasValue))
            {
                var rescuerIds = rescuerItems.Select(i => i.RescuerId).ToList();
                var incidentPoint = incident.LocationCoordinates;
                var distanceResults = await _unitOfWork.GetRepository<RescuerProfile>()
                    .GetListAsync(
                        predicate: r => rescuerIds.Contains(r.AccountId) && r.LastLocation != null,
                        selector: r => new
                        {
                            Id = r.AccountId,
                            DistanceKm = EF.Functions.Distance(r.LastLocation!, incidentPoint, true) / 1000
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
                "Snapshot on-duty rescuers built. Date={Date}, IncidentId={IncidentId}, Count={Count}, OnlyAvailable={OnlyAvailable}, MaxDistanceKm={MaxDistanceKm}",
                targetDate,
                incidentId,
                rescuerItems.Count,
                onlyAvailable,
                maxDistanceKm);

            return new OnDutyRescuerSnapshotResponse
            {
                IncidentId = incidentId,
                Date = targetDate,
                SnapshotAt = nowUtc,
                Rescuers = rescuerItems
            };
        }

        private static ShiftAssignment? SelectBestAssignment(List<ShiftAssignment> assignments, DateTime nowUtc, DateOnly targetDate)
        {
            return assignments
                .OrderByDescending(a => a.Status == ShiftAssignmentStatus.Active)
                .ThenByDescending(a => IsOnDutyNow(a, nowUtc, targetDate))
                .ThenBy(a => a.Shift.StartTime)
                .FirstOrDefault();
        }

        private static OnDutyRescuerItemResponse BuildRescuerItem(ShiftAssignment assignment, SnakebiteIncident? incident, DateTime nowUtc, double? distanceKm)
        {
            var rescuer = assignment.Rescuer;
            var isOnDutyNow = IsOnDutyNow(assignment, nowUtc, assignment.Date);

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
                ShiftStartTime = assignment.Shift.StartTime,
                ShiftEndTime = assignment.Shift.EndTime,
                ShiftDate = assignment.Date,
                Latitude = latitude,
                Longitude = longitude,
                LastLocationUpdate = rescuer.LastLocationUpdate,
                DistanceKm = distanceKm
            };
        }

        private static bool IsOnDutyNow(ShiftAssignment assignment, DateTime nowUtc, DateOnly targetDate)
        {
            if (assignment.Status == ShiftAssignmentStatus.Completed
                || assignment.Status == ShiftAssignmentStatus.Cancelled
                || assignment.Status == ShiftAssignmentStatus.NoShow)
            {
                return false;
            }

            if (assignment.Date != targetDate)
            {
                return false;
            }

            var nowTime = nowUtc.TimeOfDay;
            return IsTimeWithinShiftWindow(nowTime, assignment.Shift.StartTime, assignment.Shift.EndTime);
        }

        private static bool IsTimeWithinShiftWindow(TimeSpan current, TimeSpan start, TimeSpan end)
        {
            if (start == end)
            {
                return true;
            }

            // Overnight shift support (e.g., 22:00 -> 06:00)
            if (end < start)
            {
                return current >= start || current <= end;
            }

            return current >= start && current <= end;
        }

        public async Task<List<BriefRescuerProfileResponse>> GetRescuerRegistryAsync()
        {
            var profiles = await _unitOfWork.GetRepository<RescuerProfile>().GetListAsync(
                include: q => q.Include(p => p.Account));

            return profiles.Select(p => p.Adapt<BriefRescuerProfileResponse>()).ToList();
        }

        public async Task<BriefRescuerProfileResponse?> GetRescuerByIdAsync(Guid rescuerId)
        {
            var profile = await _unitOfWork.GetRepository<RescuerProfile>().FirstOrDefaultAsync<BriefRescuerProfileResponse>(
                predicate: p => p.AccountId == rescuerId,
                include: q => q.Include(p => p.Account),
                selector: p => p.Adapt<BriefRescuerProfileResponse>());

            return profile == null ? null : profile;
        }
    }
}
