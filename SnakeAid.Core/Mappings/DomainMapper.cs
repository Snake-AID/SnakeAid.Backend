using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mapster;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Responses.SnakebiteIncident;

namespace SnakeAid.Core.Mappings
{
    public class DomainMapper : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            // Global config for NetTopologySuite Point - use shallow copy (reference)
            config.NewConfig<NetTopologySuite.Geometries.Point, NetTopologySuite.Geometries.Point>()
                .MapWith(src => src);

            // SnakebiteIncident mappings
            config.NewConfig<SnakebiteIncident, CreateIncidentResponse>();
        }
    }
}