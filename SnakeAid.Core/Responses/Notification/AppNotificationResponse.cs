namespace SnakeAid.Core.Responses.Notification;

public class AppNotificationResponse
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string NotificationType { get; set; } = string.Empty;

    public string? DeepLink { get; set; }

    public Dictionary<string, string>? Data { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}