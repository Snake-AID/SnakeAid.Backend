using System;
using System.Collections.Generic;
using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Responses.Expert
{
    public class ExpertProfileResponse
    {
        public Guid AccountId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string Biography { get; set; } = string.Empty;
        public decimal ConsultationFee { get; set; }
        public decimal ScheduledConsultationFee { get; set; }
        public decimal EmergencyConsultationFee { get; set; }
        public decimal Rating { get; set; }
        public int RatingCount { get; set; }
        public bool IsVerified { get; set; }
        public bool IsOnline { get; set; }
        public int TotalConsultations { get; set; }
        public double? AverageResponseTimeMinutes { get; set; }
        public decimal? SuccessRate { get; set; }
        public List<string> Specializations { get; set; } = new List<string>();
    }
}
