using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Requests.Consultation;

public class ReportExpertAbsentRequest
{
    [Required]
    [StringLength(2000, MinimumLength = 1)]
    public string CustomerReport { get; set; } = string.Empty;
}
