namespace SnakeAid.Core.Requests.LiveKit;

public class LiveKitWebhookPayload
{
    public string? Id { get; set; }
    public string? Event { get; set; }
    public long CreatedAt { get; set; }
    public LiveKitWebhookRoom? Room { get; set; }
    public LiveKitWebhookParticipant? Participant { get; set; }
    public string RoomName { get; set; } = string.Empty;
}

public class LiveKitWebhookRoom
{
    public string? Name { get; set; }
    public string? Sid { get; set; }
}

public class LiveKitWebhookParticipant
{
    public string? Identity { get; set; }
    public string? Sid { get; set; }
    public string? Name { get; set; }
}
