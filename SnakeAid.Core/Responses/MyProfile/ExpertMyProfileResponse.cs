using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Responses.MyProfile;

public class ExpertMyProfileResponse
{
    public Guid AccountId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? AvatarUrl { get; set; }
    public AccountRole Role { get; set; }
    public bool IsActive { get; set; }
    public int ReputationPoints { get; set; }
    public ReputationStatus ReputationStatus { get; set; }
    public string Biography { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public decimal ScheduledConsultationFee { get; set; }
    public decimal EmergencyConsultationFee { get; set; }
    public decimal Rating { get; set; }
    public int RatingCount { get; set; }
    public bool IsVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
