using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Requests.Blog
{
    public class UpdateBlogStatusRequest
    {
        public BlogStatus Status { get; set; }

        public string? RejectionReason { get; set; }
    }
}
