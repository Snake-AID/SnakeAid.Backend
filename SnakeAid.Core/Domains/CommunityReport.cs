using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;

namespace SnakeAid.Core.Domains
{
    public class CommunityReport : BaseEntity, IHasReportMedia
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey(nameof(User))]
        public Guid UserId { get; set; }

        [Required]
        [Column(TypeName = "geometry(Point, 4326)")]
        public Point LocationCoordinates { get; set; }

        public string? Notes { get; set; }

        public int? SnakeSpeciesId { get; set; }


        // Navigation properties
        public SnakeSpecies? SnakeSpecies { get; set; }
        public Account User { get; set; }
        public ICollection<ReportMedia> Media { get; set; } = new List<ReportMedia>();
    }
}