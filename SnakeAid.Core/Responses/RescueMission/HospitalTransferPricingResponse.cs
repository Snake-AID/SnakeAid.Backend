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

        public bool RequiresHospitalization { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
