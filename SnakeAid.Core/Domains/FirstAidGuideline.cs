using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SnakeAid.Core.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SnakeAid.Core.Domains
{
    public class FirstAidGuideline : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; }

        [Required]
        [Column(TypeName = "jsonb")]
        public FirstAidContent Content { get; set; }

        [Required]
        public GuidelineType Type { get; set; } = GuidelineType.General;

        [MaxLength(500)]
        public string? Summary { get; set; }  // Tóm tắt ngắn

    }

    public class FirstAidContent
    {
        public List<FirstAidStep> Steps { get; set; } = new();
        public List<FirstAidStep> Dos { get; set; } = new();
        public List<FirstAidStep> Donts { get; set; } = new();
        public List<string> Notes { get; set; } = new();
    }

    public class FirstAidStep
    {
        public string Text { get; set; }
        public string MediaUrl { get; set; }
    }
}