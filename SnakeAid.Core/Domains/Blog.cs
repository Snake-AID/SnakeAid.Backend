namespace SnakeAid.Core.Domains
{
    public class Blog : BaseEntity
    {
        public Guid Id { get; set; }
        public Guid AuthorId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public BlogStatus Status { get; set; } = BlogStatus.Draft;
        public string RejectionReason { get; set; }
    }

    public enum BlogStatus
    {
        Draft = 0,
        PendingApproval = 1,
        Published = 2,
        Rejected = 3,
    }
}