using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Domains
{
    public class SystemSetting : BaseEntity
    {
        [Key]
        [MaxLength(100)]
        public string SettingKey { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Value { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        public SettingValueType ValueType { get; set; }
    }

    public enum SettingValueType
    {
        String = 0,
        Int = 1,
        Decimal = 2,
        Boolean = 3,
        Json = 4
    }
}