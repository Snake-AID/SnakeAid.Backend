using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Responses.TreatmentFacility
{
    public class TreatmentFacilityResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string ContactNumber { get; set; }
        public double DistanceKm { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }

    }
}