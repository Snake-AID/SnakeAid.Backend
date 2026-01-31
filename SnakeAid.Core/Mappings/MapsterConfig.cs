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

        // Scan and register all mapping configurations in the assembly
        // This will automatically find all classes implementing IRegister
        TypeAdapterConfig.GlobalSettings.Scan(Assembly.GetExecutingAssembly());
    }
}