namespace SnakeAid.Core.Domains
{
    public class UserFeedback
    {
        public Guid Id { get; set; }
        public Guid RaterId { get; set; }
        public Guid TargetUserId { get; set; }
        public Guid DocumentId { get; set; }
        public FeedbackType Type { get; set; }
        public int Rating { get; set; }
        public string Comments { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum FeedbackType
    {
        Emergency,
        Catching,
        Consultation
    }
}