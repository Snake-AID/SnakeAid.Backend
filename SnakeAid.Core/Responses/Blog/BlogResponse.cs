using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Responses.Blog
{
    public class BlogResponse
    {
        public Guid Id { get; set; }

        public Guid AuthorId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string ThumbnailUrl { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public BlogCategory Category { get; set; }

        public List<BlogTag> Tags { get; set; } = new();

        public int ViewCount { get; set; }

        public int LikeCount { get; set; }

        public int ReadingTime { get; set; }

        public BlogStatus Status { get; set; }

        public string? RejectionReason { get; set; }

        public List<string> LikedViewer { get; set; } = new();

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public BlogAuthorResponse? Account { get; set; }
    }

    public class BlogAuthorResponse
    {
        public Guid Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string? AvatarUrl { get; set; }

        public AccountRole Role { get; set; }

        public bool IsActive { get; set; }
    }
}
