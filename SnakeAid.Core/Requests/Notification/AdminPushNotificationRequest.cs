using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Requests.Notification;

public class AdminPushNotificationRequest
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Body { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Type { get; set; } = "Admin";

    public Dictionary<string, string>? Data { get; set; }
}
