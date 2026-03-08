using System;
using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Requests.Expert
{
    public class ExpertSettingsRequest
    {
        [Required]
        [MaxLength(2000)]
        public string Biography { get; set; } = string.Empty;

        [Range(typeof(decimal), "0", "999999.99")]
        public decimal? ConsultationFee { get; set; }

        [Range(typeof(decimal), "0", "999999.99")]
        public decimal? ScheduledConsultationFee { get; set; }

        [Range(typeof(decimal), "0", "999999.99")]
        public decimal? EmergencyConsultationFee { get; set; }
    }
}
