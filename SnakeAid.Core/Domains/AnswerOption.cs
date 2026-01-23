using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Domains
{
    public class AnswerOption : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(500)]
        public string OptionText { get; set; }

        [Required]
        [StringLength(1000)]
        public string OptionImageUrl { get; set; }

        [Required]
        [ForeignKey(nameof(Question))]
        public int QuestionId { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        public FilterQuestion Question { get; set; }
        public ICollection<SpeciesFilterMapping> SpeciesFilterMappings { get; set; }
    }
}