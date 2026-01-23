namespace SnakeAid.Core.Domains
{
    public class ReputationTransaction
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}