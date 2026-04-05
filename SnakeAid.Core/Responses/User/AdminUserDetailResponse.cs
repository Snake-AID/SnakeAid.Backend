using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Responses.User
{
    /// <summary>
    /// Admin detail response for user (full info)
    /// </summary>
    public class AdminUserDetailResponse
    {
        public Guid Id { get; set; }

        public string UserName { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }

        public AccountRole Role { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public bool IsActive { get; set; }

        public int ReputationPoints { get; set; }

        public ReputationStatus ReputationStatus { get; set; }

        public DateTime? SuspendedUntil { get; set; }

        public string? SuspensionReason { get; set; }

        public string? AvatarUrl { get; set; }

        public AdminMemberProfileResponse? MemberProfile { get; set; }

        public AdminExpertProfileResponse? ExpertProfile { get; set; }

        public AdminRescuerProfileResponse? RescuerProfile { get; set; }
    }

    public class AdminMemberProfileResponse
    {
        public float Rating { get; set; }

        public int RatingCount { get; set; }

        public bool HasUnderlyingDisease { get; set; }

        public List<string> EmergencyContacts { get; set; } = new List<string>();
    }

    public class AdminExpertProfileResponse
    {
        public string Biography { get; set; } = string.Empty;

        public bool IsOnline { get; set; }

        public decimal ConsultationFee { get; set; }

        public decimal? EmergencyConsultationFee { get; set; }

        public decimal Rating { get; set; }

        public int RatingCount { get; set; }
    }

    public class AdminRescuerProfileResponse
    {
        public bool IsOnline { get; set; }

        public bool IsAvailable { get; set; }

        public RescuerType Type { get; set; }

        public decimal Rating { get; set; }

        public int RatingCount { get; set; }

        public int TotalMissions { get; set; }

        public int CompletedMissions { get; set; }

        public DateTime? LastLocationUpdate { get; set; }
    }
}
