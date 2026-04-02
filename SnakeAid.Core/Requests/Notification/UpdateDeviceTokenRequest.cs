using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Requests.Notification;

public class UpdateDeviceTokenRequest
{
    [Required]
    [MaxLength(512)]
    public string DeviceToken { get; set; } = string.Empty;
}
