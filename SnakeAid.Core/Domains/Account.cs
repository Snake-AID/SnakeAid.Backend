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
        public string AvatarUrl { get; set; }

        [Required]
        public AccountRole Role { get; set; } = AccountRole.User;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public bool IsActive { get; set; } = true;

        public MemberProfile MemberProfile { get; set; }
        public ExpertProfile ExpertProfile { get; set; }
        public RescuerProfile RescuerProfile { get; set; }
    }

    public class ApplicationRole : IdentityRole<Guid>
    {
        public ApplicationRole() { }
        public ApplicationRole(string roleName) : base(roleName) { }
    }

    public enum AccountRole
    {
        User = 0,
        Admin = 1,
        Expert = 2,
        Rescuer = 3,
    }
}