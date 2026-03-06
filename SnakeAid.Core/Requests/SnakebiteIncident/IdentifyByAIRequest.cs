using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Requests.SnakebiteIncident;

/// <summary>
/// Request để xác định rắn bằng AI recognition result
/// </summary>
public class IdentifyByAIRequest
{
    [Required]
    public Guid RecognitionResultId { get; set; }
}
