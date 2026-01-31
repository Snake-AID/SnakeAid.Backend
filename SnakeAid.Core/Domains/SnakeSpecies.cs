using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SnakeAid.Core.Domains
{
    public class SnakeSpecies : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(500)]
        public string ScientificName { get; set; }

        [Required]
        [MaxLength(200)]
        public string Slug { get; set; }

        [MaxLength(500)]
        public string CommonName { get; set; }

        [MaxLength(2000)]
        public string Description { get; set; }

        [MaxLength(2000)]
        public string IdentificationSummary { get; set; }

        public PrimaryVenomType? PrimaryVenomType { get; set; }

        [Column(TypeName = "jsonb")]
        public IdentificationFeature Identification { get; set; }

        [Column(TypeName = "jsonb")]
        public List<SymptomTimeline> SymptomsByTime { get; set; }

        [Column(TypeName = "jsonb")]
        public FirstAidOverride FirstAidGuidelineOverride { get; set; }

        [Range(0.0, 10.0)]
        public float RiskLevel { get; set; }

        [Required]
        public bool IsVenomous { get; set; } = false;

        [Required]
        public bool IsActive { get; set; } = true;


        // Navigation properties
        public ICollection<FilterSnakeMapping> FilterSnakeMappings { get; set; } = new List<FilterSnakeMapping>();
        public ICollection<SpeciesAntivenom> SpeciesAntivenoms { get; set; } = new List<SpeciesAntivenom>();
        public ICollection<SpeciesVenom> SpeciesVenoms { get; set; } = new List<SpeciesVenom>();
        public ICollection<SnakeCatchingTariff> SnakeCatchingTariffs { get; set; } = new List<SnakeCatchingTariff>();
        public ICollection<SnakeSpeciesName> AlternativeNames { get; set; } = new List<SnakeSpeciesName>();
    }

    public enum PrimaryVenomType
    {
        Neurotoxic = 0,
        Hemotoxic = 1,
        Cytotoxic = 2,
        Myotoxic = 3,
        None = 4,
    }

    public class IdentificationFeature
    {
        public List<string> PhysicalTraits { get; set; } // Thân, đầu, vảy...
        public List<string> Behaviors { get; set; }     // Hoạt động đêm, chui vào nhà...
        public string Habitat { get; set; }             // Khu vực sống
    }

    // 2. Triệu chứng theo mốc thời gian (Quan trọng nhất)
    public class SymptomTimeline
    {
        public string TimeRange { get; set; }           // "0 - 15 phút", "3 - 6 giờ"
        public List<string> Signs { get; set; }         // Danh sách triệu chứng
        public bool IsCritical { get; set; }            // Đánh dấu nếu là giai đoạn nguy hiểm
    }

    public enum OverrideMode
    {
        Append = 0,
        Replace = 1
    }

    public class FirstAidOverride
    {
        public OverrideMode Mode { get; set; } = OverrideMode.Append;
        public List<string> Steps { get; set; } = new();
    }
}