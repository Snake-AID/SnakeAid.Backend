using System;
using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Requests.RescueMission
{
    public class ReportHospitalTransferRequest
    {
        [Required(ErrorMessage = "Hospital ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Valid Hospital ID must be provided")]
        public int HospitalId { get; set; }

        [Required(ErrorMessage = "Distance to hospital is required")]
        [Range(0.1, 500, ErrorMessage = "Distance must be between 0.1 and 500 km")]
        public decimal DistanceToHospitalKm { get; set; }

        [MaxLength(1000, ErrorMessage = "Notes must not exceed 1000 characters")]
        public string? Notes { get; set; }
    }
}
