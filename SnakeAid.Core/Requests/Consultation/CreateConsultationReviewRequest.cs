using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Requests.Consultation;

public class CreateConsultationReviewRequest
{
    [Required]
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
    public int Rating { get; set; }

    [MaxLength(2000)]
    public string? Comments { get; set; }
}
