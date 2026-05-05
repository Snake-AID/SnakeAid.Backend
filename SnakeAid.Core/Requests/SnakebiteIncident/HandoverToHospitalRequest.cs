using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Requests.SnakebiteIncident
{
    public class HandoverToHospitalRequest
    {
        [Required(ErrorMessage = "Hospital name is required.")]
        [MaxLength(200, ErrorMessage = "Hospital name cannot exceed 200 characters.")]
        public string HospitalName { get; set; } = string.Empty;

        [MaxLength(50, ErrorMessage = "Hospital phone cannot exceed 50 characters.")]
        public string? HospitalPhone { get; set; }

        [MaxLength(1000, ErrorMessage = "Operator note cannot exceed 1000 characters.")]
        public string? Note { get; set; }
    }
}