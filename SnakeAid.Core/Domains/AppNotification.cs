using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Domains
{
    public class AppNotification : BaseEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey(nameof(User))]
        public Guid UserId { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Message { get; set; }

        [Required]
        [MaxLength(100)]
        public string NotificationType { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? DeepLink { get; set; }

        [MaxLength(4000)]
        public string? PayloadJson { get; set; }

        public bool IsRead { get; set; } = false;

        // Navigation property
        public Account User { get; set; }
    }
}