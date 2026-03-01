using System;
using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Responses.UserFeedback
{
    public class UserFeedbackResponse
    {
        public Guid Id { get; set; }

        public Guid RaterId { get; set; }

        public Guid TargetUserId { get; set; }

        public Guid ReferenceId { get; set; }

        public FeedbackType Type { get; set; }

        public int Rating { get; set; }

        public string? Comments { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        // Additional info
        public string? RaterName { get; set; }

        public string? TargetUserName { get; set; }

        public decimal UpdatedAverageRating { get; set; }

        public int UpdatedRatingCount { get; set; }
    }
}
