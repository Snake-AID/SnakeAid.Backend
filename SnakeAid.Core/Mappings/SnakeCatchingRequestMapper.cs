using Mapster;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Responses.Auth;
using SnakeAid.Core.Responses.MemberProfile;
using SnakeAid.Core.Responses.RescuerProfile;
using SnakeAid.Core.Responses.SnakeCatchingRequest;

namespace SnakeAid.Core.Mappings
{
    public class SnakeCatchingRequestMapper : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            TypeAdapterConfig<SnakeCatchingRequest, CreateSnakeCatchingRequestResponse>
                .NewConfig()
                .PreserveReference(true) // Enable circular reference handling
                .MaxDepth(5) // Increase depth to allow Media mapping
                .Map(dest => dest.LocationCoordinates, src => src.LocationCoordinates)
                .Map(dest => dest.Lng, src => src.LocationCoordinates.X)
                .Map(dest => dest.Lat, src => src.LocationCoordinates.Y)
                .Map(dest => dest.HandlingOperator, src => src.HandlingOperator)
                .Map(dest => dest.Media, src => src.Media); // Explicitly map Media

            TypeAdapterConfig<SnakeCatchingRequest, DetailSnakeCatchingRequestResponse>
                .NewConfig()
                .PreserveReference(true) // Enable circular reference handling
                .MaxDepth(5) // Increase depth to allow Media mapping
                .Map(dest => dest.LocationCoordinates, src => src.LocationCoordinates)
                .Map(dest => dest.Lng, src => src.LocationCoordinates.X)
                .Map(dest => dest.Lat, src => src.LocationCoordinates.Y)
                .Map(dest => dest.HandlingOperator, src => src.HandlingOperator)
                .Map(dest => dest.Media, src => src.Media); // Explicitly map Media
        }
    }
}
