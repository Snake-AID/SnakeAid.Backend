using Mapster;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Responses.SnakeCatchingRequest;

namespace SnakeAid.Core.Mappings
{
    public class SnakeCatchingRequestMapper : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            TypeAdapterConfig<SnakeCatchingRequest, CreateSnakeCatchingRequestResponse>
                .NewConfig()
                .Map(dest => dest.LocationCoordinates, src => src.LocationCoordinates)
                .Map(dest => dest.Lng, src => src.LocationCoordinates.X)
                .Map(dest => dest.Lat, src => src.LocationCoordinates.Y);
        }
    }
}
