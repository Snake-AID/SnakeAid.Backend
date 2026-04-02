using System.ComponentModel.DataAnnotations;
using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Requests.Notification;

public class AdminBroadcastNotificationRequest
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Body { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Type { get; set; } = "Admin";

    public List<AccountRole>? TargetRoles { get; set; }

    public Dictionary<string, string>? Data { get; set; }
}