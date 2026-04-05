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

        public string? FcmToken { get; set; }

        public bool EmailConfirmed { get; set; }

        public bool PhoneNumberConfirmed { get; set; }

        public int? AccessFailedCount { get; set; }

        public DateTime? LockoutEnd { get; set; }

        public bool LockoutEnabled { get; set; }

        public bool TwoFactorEnabled { get; set; }
    }
}
