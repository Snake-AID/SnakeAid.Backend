using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mapster;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Responses.Auth;
using SnakeAid.Core.Responses.MemberProfile;
using SnakeAid.Core.Responses.RescuerProfile;

namespace SnakeAid.Core.Mappings
{
    public class AccountMapper : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
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

            TypeAdapterConfig<Account, UserInfo>
                .NewConfig();

            // Map Account → UserInfo
            config.NewConfig<Account, UserInfo>()
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.Role, src => src.Role.ToString());

            // Map MemberProfile → BriefMemberProfileResponse
            config.NewConfig<MemberProfile, BriefMemberProfileResponse>()
                .Map(dest => dest.AccountId, src => src.AccountId)
                .Map(dest => dest.Account, src => src.Account)
                .Map(dest => dest.UserName, src => src.Account.UserName)
                .Map(dest => dest.Email, src => src.Account.Email)
                .Map(dest => dest.PhoneNumber, src => src.Account.PhoneNumber);

            // Map RescuerProfile → BriefRescuerProfileResponse
            config.NewConfig<RescuerProfile, BriefRescuerProfileResponse>()
                .Map(dest => dest.AccountId, src => src.AccountId)
                .Map(dest => dest.Account, src => src.Account)
                .Map(dest => dest.PhoneNumber, src => src.Account.PhoneNumber)
                .Map(dest => dest.Latitude, src => src.LastLocation != null ? (double?)src.LastLocation.Y : null)
                .Map(dest => dest.Longitude, src => src.LastLocation != null ? (double?)src.LastLocation.X : null);
        }
    }
}