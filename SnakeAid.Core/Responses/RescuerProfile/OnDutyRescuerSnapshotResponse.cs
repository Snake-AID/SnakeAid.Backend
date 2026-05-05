using System;
using System.Collections.Generic;

namespace SnakeAid.Core.Responses.RescuerProfile
{
    public class OnDutyRescuerSnapshotResponse
    {
        public Guid? ContextId { get; set; }
        public DateOnly Date { get; set; }
        public DateTime SnapshotAt { get; set; }
        public List<OnDutyRescuerItemResponse> Rescuers { get; set; } = new();
    }

    public class OnDutyRescuerItemResponse
    {
        public Guid RescuerId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public bool IsOnline { get; set; }
        public bool IsAvailable { get; set; }
        public bool IsOnDutyNow { get; set; }
        public string AssignmentStatus { get; set; } = string.Empty;
        public Guid ShiftAssignmentId { get; set; }
        public Guid ShiftId { get; set; }
        public string ShiftName { get; set; } = string.Empty;
        public TimeSpan ShiftStartTime { get; set; }
        public TimeSpan ShiftEndTime { get; set; }
        public DateOnly ShiftDate { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public DateTime? LastLocationUpdate { get; set; }
        public double? DistanceKm { get; set; }
    }

    public class OffDutyRescuerSnapshotResponse
    {
        public Guid? ContextId { get; set; }
        public DateTime SnapshotAt { get; set; }
        public List<OffDutyRescuerItemResponse> Rescuers { get; set; } = new();
    }

    public class OffDutyRescuerItemResponse
    {
        public Guid RescuerId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public bool IsOnline { get; set; }
        public bool IsAvailable { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public DateTime? LastLocationUpdate { get; set; }
        public double? DistanceKm { get; set; }
    }
}