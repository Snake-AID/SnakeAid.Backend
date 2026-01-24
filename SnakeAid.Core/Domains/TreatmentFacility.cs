using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;

namespace SnakeAid.Core.Domains
{
    public class TreatmentFacility : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        [Required]
        [MaxLength(500)]
        public string Address { get; set; }

        [Required]
        [MaxLength(20)]
        [Phone]
        public string ContactNumber { get; set; }


        [Required]
        [Column(TypeName = "geometry(Point, 4326)")]
        public Point Location { get; set; }


        [Required]
        public bool IsActive { get; set; } = true;


        // Navigation properties
        public ICollection<Antivenom> AntivenomStocks { get; set; } = new List<Antivenom>();
    }
}