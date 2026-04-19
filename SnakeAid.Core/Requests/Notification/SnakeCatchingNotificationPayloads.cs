using System;

namespace SnakeAid.Core.Requests.Notification;

public class DispatchRequestNotificationPayload
{
    public Guid RequestId { get; set; }
    public Guid IncidentId { get; set; }
    public Guid OperatorId { get; set; }
    public Guid RescuerId { get; set; }
    public DateTime DispatchedAt { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Message { get; set; }
}

public class RescuerRequestNotificationPayload
{
    public Guid RequestId { get; set; }
    public Guid RescuerId { get; set; }
    public string? ReasonCode { get; set; }
    public string? Message { get; set; }
}

public class MissionStartedNotificationPayload
{
    public string? Status { get; set; }
    public string? RescuerName { get; set; }
    public int? EstimatedMinutes { get; set; }
}

public class MissionCompletedNotificationPayload
{
    public Guid MissionId { get; set; }
    public decimal? ActualCost { get; set; }
    public string? RescuerName { get; set; }
}
