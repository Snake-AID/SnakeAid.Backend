using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Requests.TreatmentFacility
{
    public class UpdateTreatmentFacilityRequest
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Valid Facility Id must be provided")]
        public int Id { get; set; }

        [Required(ErrorMessage = "Facility Name must be provided")]
        [MaxLength(200)]
        public string Name { get; set; }

        [Required(ErrorMessage = "Address must be provided")]
        [MaxLength(500)]
        public string Address { get; set; }

        [Required(ErrorMessage = "Contact Number must be provided")]
        [MaxLength(20)]
        [Phone]
        public string ContactNumber { get; set; }

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        public bool IsActive { get; set; } = true;
    }
}