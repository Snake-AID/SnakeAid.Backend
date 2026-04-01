namespace SnakeAid.Core.Responses.SnakeSpecies
{
    /// <summary>
    /// Full region info including GeoJSON polygon for map rendering.
    /// This model is geometry-only and does not include snake mapping state.
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
