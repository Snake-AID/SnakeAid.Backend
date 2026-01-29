using Mapster;
using NetTopologySuite.Geometries;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Responses.SnakebiteIncident;
using System;
using System.Reflection;

namespace SnakeAid.Core.Mappings;

public static class MapsterConfig
{
    public static void RegisterMappings()
    {
        // Configure mapping for NetTopologySuite Point (geometry)
        TypeAdapterConfig<SnakebiteIncident, CreateIncidentResponse>
            .NewConfig()
            .Map(dest => dest.LocationCoordinates, src => src.LocationCoordinates)
            .Map(dest => dest.Location, src => src.Location);

        // Scan and register all mapping configurations in the assembly
        // This will automatically find all classes implementing IRegister
        TypeAdapterConfig.GlobalSettings.Scan(Assembly.GetExecutingAssembly());
    }
}