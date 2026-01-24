using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Domains
{
    public class UserFeedback : BaseEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey(nameof(Rater))]
        public Guid RaterId { get; set; }

        [Required]
        [ForeignKey(nameof(TargetUser))]
        public Guid TargetUserId { get; set; }

        [Required]
        public Guid ReferenceId { get; set; }

        [Required]
        public FeedbackType Type { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }  // 1-5 stars

        [MaxLength(2000)]
        public string? Comments { get; set; }


        // Navigation properties
        public Account Rater { get; set; }
        public Account TargetUser { get; set; }
    }

    public enum FeedbackType
    {
        Emergency = 0,    // Feedback cho rescue mission
        Catching = 1,     // Feedback cho snake catching
        Consultation = 2  // Feedback cho expert consultation
    }
}