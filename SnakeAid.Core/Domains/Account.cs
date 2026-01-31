using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace SnakeAid.Core.Domains
{
    public class Account : IdentityUser<Guid>
    {
        [Required]
        [MaxLength(200)]
        public string FullName { get; set; }

        [MaxLength(1000)]
        public string? AvatarUrl { get; set; }

        [Required]
        public AccountRole Role { get; set; } = AccountRole.User;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public bool IsActive { get; set; } = false;

        // Reputation fields
        [Required]
        public int ReputationPoints { get; set; } = 100; // Điểm khởi điểm

        [Required]
        public ReputationStatus ReputationStatus { get; set; } = ReputationStatus.Good;

        public DateTime? SuspendedUntil { get; set; }  // Nếu bị tạm khóa

        public string? SuspensionReason { get; set; }

        public MemberProfile? MemberProfile { get; set; }
        public ExpertProfile? ExpertProfile { get; set; }
        public RescuerProfile? RescuerProfile { get; set; }
        public ICollection<Otp> Otps { get; set; } = new List<Otp>();
    }

    public enum AccountRole
    {
        User = 0,
        Admin = 1,
        Expert = 2,
        Rescuer = 3,
    }

    public enum ReputationStatus
    {
        Excellent = 0,  // 90+ điểm
        Good = 1,       // 70-89 điểm
        Average = 2,    // 50-69 điểm
        Poor = 3,       // 30-49 điểm
        Suspended = 4   // < 30 điểm hoặc vi phạm nghiêm trọng
    }
}