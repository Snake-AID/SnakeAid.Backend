using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Domains
{
    public class BaseEntity
    {
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}