using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Requests.AIRecognition;

public class ExpertVerifyRecognitionRequest
{
    [Required]
    [Range(1, int.MaxValue)]
    public int CorrectedSpeciesId { get; set; }

    [MaxLength(1000)]
    public string? ExpertNotes { get; set; }
}
