using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Requests.CommunityReport
{
    public class CreateCommunityReportRequest
    {
        [Required]
        [Range(-180, 180)]
        public double Longitude { get; set; }

        [Required]
        [Range(-90, 90)]
        public double Latitude { get; set; }

        [MaxLength(2000)]
        public string? AdditionalDetails { get; set; }
    }
}
