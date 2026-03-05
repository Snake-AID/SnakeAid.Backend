using System;
using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Requests.Expert
{
    public class ExpertSettingsRequest
    {
        [Required]
        [MaxLength(2000)]
        public string Biography { get; set; } = string.Empty;

        [Required]
        [Range(0, 999999.99)]
        public decimal ConsultationFee { get; set; }
    }
}
