using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Responses.RescueMission
{
    public class HospitalTransferResponse
    {
        public int HospitalId { get; set; }
        public string HospitalName { get; set; } = string.Empty;
        public string Address { get; set; }
        public string ContactNumber { get; set; }
    }
}