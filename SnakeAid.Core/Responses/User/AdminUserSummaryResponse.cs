using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Responses.User
{
    /// <summary>
    /// Admin list response for users (lightweight, no nested profiles)
    /// </summary>
    public class AdminUserSummaryResponse
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
    }
}
