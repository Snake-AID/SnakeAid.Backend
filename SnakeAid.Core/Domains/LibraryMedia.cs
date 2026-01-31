using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Domains
{
    public class LibraryMedia : BaseEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(2000)]
        [Url]
        public string MediaUrl { get; set; }

        [Required]
        public MediaType MediaType { get; set; }

        [MaxLength(200)]
        public string? FileName { get; set; }

        [Range(1, long.MaxValue)]
        public long? FileSizeBytes { get; set; }

        [MaxLength(50)]
        public string? ContentType { get; set; }  // MIME type

        [Required]
        public bool IsActive { get; set; } = true;

        [Required]
        public bool IsPublic { get; set; } = true;

        [ForeignKey(nameof(UploadedBy))]
        public Guid? UploadedById { get; set; }

        public DateTime? UploadedAt { get; set; } = DateTime.UtcNow;


        public Account? UploadedBy { get; set; }
    }

    public enum MediaType
    {
        Image = 0,
        Video = 1,
        Document = 2
    }
}