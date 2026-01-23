using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Domains
{
    public class ChatMessage
    {
        public Guid Id { get; set; }
        public Guid ConsultationId { get; set; }
        public Guid SenderId { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Content { get; set; }

        public DateTime SentAt { get; set; }

        public string? AttachmentUrl { get; set; }

        public Consultation Consultation { get; set; }
        public Account Sender { get; set; }
    }
}