using System.Reflection;
using Mapster;

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