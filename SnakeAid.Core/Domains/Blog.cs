using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Domains
{
    public class Blog : BaseEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey(nameof(Author))]
        public Guid AuthorId { get; set; }

        [Required]
        [StringLength(500)]
        public string Title { get; set; }

        [Required]
        [StringLength(500)]
        public string ThumbnailUrl { get; set; }

        [Required]
        public string Content { get; set; }

        [Required]
        public BlogCategory Category { get; set; }

        [Required]
        public List<BlogTag> Tags { get; set; } = new List<BlogTag>();

        [Required]
        public int ViewCount { get; set; } = 0;

        [Required]
        public int LikeCount { get; set; } = 0;

        [Required]
        public int ReadingTime { get; set; } // in minutes

        [Required]
        public BlogStatus Status { get; set; } = BlogStatus.Draft;

        public string? RejectionReason { get; set; }

        public List<string> LikedViewer { get; set; } = new List<string>();

        // Navigation properties
        public Account Author { get; set; }
    }

    public enum BlogStatus
    {
        Draft = 0,
        PendingApproval = 1,
        Published = 2,
        Rejected = 3,
    }

    public enum BlogCategory
    {
        SnakeKnowledge = 0,
        SnakeSpecies = 1,
        SnakeHealth = 2,
        SnakeFeeding = 3,
        SnakeHabitat = 4,
        Other = 5
    }

    public enum BlogTag
    {
        Venomous = 0,
        NonVenomous = 1,
        Safety = 2,
        WildSnake = 3,
        SnakeCare = 4,
        SnakeBehavior = 5,
        SnakeIdentification = 6,
        SnakeConservation = 7,
        SnakeMyths = 8,
        Other = 9
    }
}