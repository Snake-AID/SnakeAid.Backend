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
    }
}
