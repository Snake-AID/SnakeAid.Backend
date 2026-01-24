using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Domains
{
    public class VenomType : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(200)]
        public string? ScientificName { get; set; }  // Tên khoa học

        [Required]
        [MaxLength(1000)]
        public string Description { get; set; }


        [Required]
        public bool IsActive { get; set; } = true;

        [Range(1, 10)]
        public int? SeverityIndex { get; set; }  // Chỉ số mức độ nguy hiểm (1-10)

        [ForeignKey(nameof(FirstAidGuideline))]
        [Required]
        public int FirstAidGuidelineId { get; set; }

        // Navigation properties
        public ICollection<SpeciesVenom> SpeciesVenoms { get; set; } = new List<SpeciesVenom>();
        public ICollection<SymptomConfig> SymptomConfigs { get; set; } = new List<SymptomConfig>();

        public FirstAidGuideline FirstAidGuideline { get; set; }
    }

}