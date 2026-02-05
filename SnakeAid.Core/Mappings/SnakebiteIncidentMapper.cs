using Mapster;
using NetTopologySuite.Geometries;
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
            // Map Point → GeoPointResponse
            config.NewConfig<Point, GeoPointResponse>()
                .Map(dest => dest.Latitude, src => src.Y)
                .Map(dest => dest.Longitude, src => src.X);

            // Map Incident → Response
            config.NewConfig<SnakebiteIncident, CreateIncidentResponse>()
                .Map(dest => dest.LocationCoordinates, src => src.LocationCoordinates);
        }
    }
}
