using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Responses.Blog
{
    public class BlogResponse
    {
        public Guid Id { get; set; }

        public Guid AuthorId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public BlogStatus Status { get; set; }

        public string? RejectionReason { get; set; }

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
