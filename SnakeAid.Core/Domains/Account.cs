using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace SnakeAid.Core.Domains
{
    public class Account : IdentityUser<Guid>, IBaseEntity
    {
        public string FullName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public bool PhoneVerified { get; set; }
        public bool IsActive { get; set; } = true;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum AccountRole
    {
        User = 0,
        Admin = 1,
        Expert = 2,
        Rescuer = 3,
    }
}
