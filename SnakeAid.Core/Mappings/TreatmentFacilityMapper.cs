using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mapster;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Responses.TreatmentFacility;

namespace SnakeAid.Core.Mappings
{
    public class TreatmentFacilityMapper : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<TreatmentFacility, TreatmentFacilityResponse>()
                .Map(dest => dest.Latitude, src => src.Location.Y)
                .Map(dest => dest.Longitude, src => src.Location.X);
            // distance depends on the query point and is calculated in service
        }
    }
}