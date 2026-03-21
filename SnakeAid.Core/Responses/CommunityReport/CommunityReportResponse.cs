using SnakeAid.Core.Responses.SnakeSpecies;

namespace SnakeAid.Core.Responses.CommunityReport
{
    public class CommunityReportResponse
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string? ReporterName { get; set; }
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public string? Notes { get; set; }
        public SnakeSpeciesResponse? SnakeSpecies { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
