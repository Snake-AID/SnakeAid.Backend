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
        // Configure global settings to handle circular references
        TypeAdapterConfig.GlobalSettings.Default
            .PreserveReference(true) // Enable reference tracking globally
            .MaxDepth(3); // Limit mapping depth to prevent stack overflow

        // Scan and register all mapping configurations in the assembly
        // This will automatically find all classes implementing IRegister
        TypeAdapterConfig.GlobalSettings.Scan(Assembly.GetExecutingAssembly());
    }
}