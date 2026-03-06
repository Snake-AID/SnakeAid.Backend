using System.ComponentModel.DataAnnotations;
using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Requests.SnakebiteIncident;

/// <summary>
/// Request để xác định rắn bằng cách trả lời câu hỏi filter
/// </summary>
public class IdentifyByFilterRequest
{
    [Required]
    public List<FilterAnswerItem> Answers { get; set; } = new();
    
    /// <summary>
    /// Nếu có nhiều kết quả khớp, user chọn species ID cuối cùng
    /// </summary>
    public int? SelectedSnakeSpeciesId { get; set; }
}

public class FilterAnswerItem
{
    [Required]
    public int QuestionId { get; set; }
    
    [Required]
    public int SelectedOptionId { get; set; }
}
