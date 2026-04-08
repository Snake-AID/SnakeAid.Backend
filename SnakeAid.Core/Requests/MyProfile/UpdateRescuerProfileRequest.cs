using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Requests.MyProfile;

public class UpdateRescuerProfileRequest
{
    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Phone]
    [MaxLength(50)]
    public string? PhoneNumber { get; set; }

    [MaxLength(1000)]
    public string? AvatarUrl { get; set; }
}
