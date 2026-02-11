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
                .PreserveReference(true) // Enable circular reference handling
                .MaxDepth(3) // Limit mapping depth to prevent infinite loops
                .Map(dest => dest.LocationCoordinates, src => src.LocationCoordinates)
                .Map(dest => dest.Lng, src => src.LocationCoordinates.X)
                .Map(dest => dest.Lat, src => src.LocationCoordinates.Y);

            // Configure MemberProfile mapping to ignore circular collections
            TypeAdapterConfig<MemberProfile, MemberProfile>
                .NewConfig()
                .PreserveReference(true)
                .MaxDepth(2);

            // Configure RescuerProfile mapping to ignore circular collections
            TypeAdapterConfig<RescuerProfile, RescuerProfile>
                .NewConfig()
                .PreserveReference(true)
                .MaxDepth(2);

            // Configure Account mapping to ignore profile back-references
            TypeAdapterConfig<Account, Account>
                .NewConfig()
                .PreserveReference(true)
                .MaxDepth(2);
        }
    }
}
