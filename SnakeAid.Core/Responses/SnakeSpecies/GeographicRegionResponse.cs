namespace SnakeAid.Core.Responses.SnakeSpecies
{
    /// <summary>
    /// Full region info including GeoJSON polygon for map rendering.
    /// When snakeSpeciesId is provided, also includes mapping state for that snake.
    /// </summary>
    public class GeographicRegionResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }

        /// <summary>
        /// Polygon boundary as GeoJSON-compatible coordinates array
        /// [ [lng, lat], [lng, lat], ... ] (closed ring)
        /// </summary>
        public List<double[]> BoundaryCoordinates { get; set; } = new();

        /// <summary>
        /// Whether this region has an active mapping for the requested snake species.
        /// Always false when snakeSpeciesId is not provided.
        /// </summary>
        public bool IsMapped { get; set; }

        /// <summary>
        /// Mapping metadata for the requested snake species, null if not mapped.
        /// </summary>
        public RegionSnakeMappingResponse? Mapping { get; set; }
    }

    /// <summary>
    /// A single region-snake mapping entry (used in snake detail view)
    /// </summary>
    public class RegionSnakeMappingResponse
    {
        public int Id { get; set; }
        public int GeographicRegionId { get; set; }
        public string RegionName { get; set; } = string.Empty;
        public string RegionCode { get; set; } = string.Empty;
        public string CommonLevel { get; set; } = string.Empty;
        public int CommonLevelValue { get; set; }
        public int Priority { get; set; }
        public string? DistributionNotes { get; set; }
        public bool IsActive { get; set; }
    }
}
