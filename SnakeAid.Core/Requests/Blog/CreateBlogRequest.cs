using SnakeAid.Core.Domains;
using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Requests.Blog
{
    public class CreateBlogRequest
    {
        [Required]
        [StringLength(500)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string ThumbnailUrl { get; set; } = string.Empty;

        [Required]
        public BlogCategory Category { get; set; }

        [Required]
        public List<BlogTag> Tags { get; set; } = new();

        [Required]
        [Range(1, 1440)]
        public int ReadingTime { get; set; }
    }
}
