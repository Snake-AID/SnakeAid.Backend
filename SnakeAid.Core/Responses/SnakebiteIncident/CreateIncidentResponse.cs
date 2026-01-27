using NetTopologySuite.Geometries;
using SnakeAid.Core.Domains;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAid.Core.Responses.SnakebiteIncident
{
    public class CreateIncidentResponse
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }  // FK to MemberProfile

        [Required]
        [MaxLength(1000)]
        public string Location { get; set; }

        [Required]
        [Column(TypeName = "geometry(Point, 4326)")]
        public Point LocationCoordinates { get; set; }


        [Required]
        public SnakebiteIncidentStatus Status { get; set; } = SnakebiteIncidentStatus.Pending;

        // Session ping info
        [Required]
        public int CurrentSessionNumber { get; set; } = 0;   // Track session hiện tại

        [Required]
        [Range(1, 50)]
        public int CurrentRadiusKm { get; set; } = 5;        // Radius hiện tại

        public DateTime? IncidentOccurredAt { get; set; }  // Khi nào bị cắn

        
    }
}
