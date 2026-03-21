using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Requests.TreatmentFacility
{
    public class CreateTreatmentFacilityRequest
    {
        [Required(ErrorMessage = "Facility Name must be provided")]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address must be provided")]
        [MaxLength(500)]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact Number must be provided")]
        [MaxLength(20)]
        [Phone]
        public string ContactNumber { get; set; } = string.Empty;

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public List<int> AntivenomIds { get; set; } = new();

        public bool IsActive { get; set; } = true;
    }
}