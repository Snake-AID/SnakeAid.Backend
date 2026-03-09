using Mapster;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Responses.MemberProfile;
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
                .Map(dest => dest.Media, src => src.Media); // Explicitly map Media

            TypeAdapterConfig<SnakeCatchingRequest, DetailSnakeCatchingRequestResponse>
                .NewConfig()
                .PreserveReference(true) // Enable circular reference handling
                .MaxDepth(5) // Increase depth to allow Media mapping
                .Map(dest => dest.LocationCoordinates, src => src.LocationCoordinates)
                .Map(dest => dest.Lng, src => src.LocationCoordinates.X)
                .Map(dest => dest.Lat, src => src.LocationCoordinates.Y)
                .Map(dest => dest.Media, src => src.Media); // Explicitly map Media

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

            // Configure MemberProfile to BriefMemberProfileResponse mapping
            TypeAdapterConfig<MemberProfile, BriefMemberProfileResponse>
                .NewConfig()
                .Map(dest => dest.UserName, src => src.Account.UserName)
                .Map(dest => dest.Email, src => src.Account.Email)
                .Map(dest => dest.PhoneNumber, src => src.Account.PhoneNumber);
        }
    }
}
