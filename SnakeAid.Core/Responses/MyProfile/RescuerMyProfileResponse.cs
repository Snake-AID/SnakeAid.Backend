using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Responses.MyProfile;

public class RescuerMyProfileResponse
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
    public bool IsOnline { get; set; }
    public bool IsAvailable { get; set; }
    public RescuerType Type { get; set; }
    public decimal Rating { get; set; }
    public int RatingCount { get; set; }
    public int TotalMissions { get; set; }
    public int CompletedMissions { get; set; }
    public DateTime? LastLocationUpdate { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
