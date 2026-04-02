using System.ComponentModel.DataAnnotations;
using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Requests.SystemSetting;

public class UpsertSystemSettingRequest
{
    [Required]
    [MaxLength(2000)]
    public string Value { get; set; } = string.Empty;

    [Required]
    public SettingValueType ValueType { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }
}
