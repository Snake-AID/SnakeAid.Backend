using System.ComponentModel.DataAnnotations;
using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Requests.SnakebiteIncident;

/// <summary>
/// Request để xác định rắn bằng cách trả lời câu hỏi filter
/// User đã chọn snake species từ danh sách filtered results
/// </summary>
public class IdentifyByFilterRequest
{
    /// <summary>
    /// Danh sách option IDs mà user đã chọn (từ questionnaire)
    /// VD: [1, 5, 9, 12]
    /// </summary>
    [Required]
    [MinLength(1, ErrorMessage = "At least one answer is required")]
    public List<int> SelectedOptionIds { get; set; } = new();
    
    /// <summary>
    /// Snake species ID mà user chọn cuối cùng từ filtered results
    /// </summary>
    [Required(ErrorMessage = "Selected snake species ID is required")]
    public int SelectedSnakeSpeciesId { get; set; }
    
    /// <summary>
    /// Match score của snake được chọn (số đáp án khớp)
    /// Frontend tính toán và gửi lên để lưu vào database
    /// </summary>
    [Range(0, int.MaxValue)]
    public int MatchScore { get; set; }
    
    /// <summary>
    /// Match percentage (MatchScore / TotalAnswered * 100)
    /// Frontend tính toán và gửi lên để lưu vào database
    /// </summary>
    [Range(0, 100)]
    public double MatchPercentage { get; set; }
}
