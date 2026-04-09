using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Requests.AIRecognition;

public class ExpertRejectRecognitionRequest
{
    [MaxLength(1000)]
    public string? ExpertNotes { get; set; }
}
