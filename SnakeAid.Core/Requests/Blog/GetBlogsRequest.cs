using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Requests.Blog
{
    public class GetBlogsRequest
    {
        public Guid? AccountId { get; set; }

        public BlogStatus? Status { get; set; }

        public string? BlogName { get; set; }
    }
}
