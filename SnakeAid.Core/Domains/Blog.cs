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
        public string Content { get; set; }

        [Required]
        public BlogStatus Status { get; set; } = BlogStatus.Draft;

        public string? RejectionReason { get; set; }

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
}