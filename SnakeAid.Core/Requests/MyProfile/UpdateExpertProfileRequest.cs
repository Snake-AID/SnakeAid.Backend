using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Requests.MyProfile;

public class UpdateExpertProfileRequest
{
    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Phone]
    [MaxLength(50)]
    public string? PhoneNumber { get; set; }

    [MaxLength(1000)]
    public string? AvatarUrl { get; set; }

    [Required]
    [MaxLength(2000)]
    public string Biography { get; set; } = string.Empty;

    [Required]
    [Range(typeof(decimal), "0", "999999.99", ParseLimitsInInvariantCulture = true)]
    public decimal? ScheduledConsultationFee { get; set; }

    [Range(typeof(decimal), "0", "999999.99", ParseLimitsInInvariantCulture = true)]
    public decimal? EmergencyConsultationFee { get; set; }
}
