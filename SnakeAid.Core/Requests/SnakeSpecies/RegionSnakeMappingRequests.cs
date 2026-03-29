using System.ComponentModel.DataAnnotations;
using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Requests.SnakeSpecies
{
    /// <summary>
    /// A single region mapping entry for upsert operations
    /// </summary>
    public class RegionMappingItem
    {
        [Required]
        public int GeographicRegionId { get; set; }

        [Required]
        public CommonLevel CommonLevel { get; set; } = CommonLevel.Common;

        [Range(0, 100)]
        public int Priority { get; set; } = 0;

        [MaxLength(500)]
        public string? DistributionNotes { get; set; }

        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Sync (replace-all) region mappings for a snake species
    /// </summary>
    public class SyncRegionMappingsRequest
    {
        [Required]
        public List<RegionMappingItem> Mappings { get; set; } = new();
    }

    /// <summary>
    /// Add a single region mapping
    /// </summary>
    public class AddRegionMappingRequest
    {
        [Required]
        public int GeographicRegionId { get; set; }

        [Required]
        public CommonLevel CommonLevel { get; set; } = CommonLevel.Common;

        [Range(0, 100)]
        public int Priority { get; set; } = 0;

        [MaxLength(500)]
        public string? DistributionNotes { get; set; }
    }

    /// <summary>
    /// Update an existing region mapping
    /// </summary>
    public class UpdateRegionMappingRequest
    {
        public CommonLevel? CommonLevel { get; set; }

        [Range(0, 100)]
        public int? Priority { get; set; }

        [MaxLength(500)]
        public string? DistributionNotes { get; set; }

        public bool? IsActive { get; set; }
    }
}
