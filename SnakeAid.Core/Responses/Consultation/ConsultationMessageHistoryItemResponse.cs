namespace SnakeAid.Core.Responses.Consultation;

public class ConsultationMessageHistoryItemResponse
{
    public Guid Id { get; set; }
    public Guid ConsultationId { get; set; }
    public Guid SenderId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? AttachmentUrl { get; set; }
    public DateTime SentAt { get; set; }
}
