using System;

namespace SnakeAid.Core.Responses.RescueMission
{
    /// <summary>
    /// Response containing pricing information for hospital transfer
    /// </summary>
    public class HospitalTransferPricingResponse
    {
        public int HospitalId { get; set; }

        public string HospitalName { get; set; } = string.Empty;

        public decimal DistanceKm { get; set; }

        public decimal PricePerKm { get; set; }

        public decimal HospitalTransferPrice { get; set; }

        public decimal BaseMissionPrice { get; set; }

        public decimal TotalPrice { get; set; }

        public DateTime CalculatedAt { get; set; }
    }
}
