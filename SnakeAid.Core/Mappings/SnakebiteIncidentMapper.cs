using Mapster;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Responses.SnakebiteIncident;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAid.Core.Mappings
{
    public class SnakebiteIncidentMapper : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            TypeAdapterConfig<SnakebiteIncident, CreateIncidentResponse>
                .NewConfig()
                .Map(dest => dest.LocationCoordinates, src => src.LocationCoordinates);
        }
    }
}
