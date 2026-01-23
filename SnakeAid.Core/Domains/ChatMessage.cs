using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Domains
{
    public class ChatMessage
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey(nameof(Consultation))]
        public Guid ConsultationId { get; set; }

        [Required]
        [ForeignKey(nameof(Sender))]
        public Guid SenderId { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Content { get; set; }

        [Required]
        public DateTime SentAt { get; set; }

        public string? AttachmentUrl { get; set; }

        public Consultation Consultation { get; set; }
        public Account Sender { get; set; }
    }
}