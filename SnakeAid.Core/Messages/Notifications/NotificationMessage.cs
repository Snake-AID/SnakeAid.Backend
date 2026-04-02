namespace SnakeAid.Core.Messages.Notifications;

public class NotificationMessage
{
    public Guid NotificationId { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Type { get; set; } = "System";
    public Dictionary<string, string>? Data { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
