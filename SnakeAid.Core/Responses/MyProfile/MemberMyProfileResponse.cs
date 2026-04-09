using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Responses.MyProfile;

public class MemberMyProfileResponse
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
    public float Rating { get; set; }
    public int RatingCount { get; set; }
    public List<string> EmergencyContacts { get; set; } = new();
    public bool HasUnderlyingDisease { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
