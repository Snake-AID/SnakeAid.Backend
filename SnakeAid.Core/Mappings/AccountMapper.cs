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
            // Map Account → UserInfo
            config.NewConfig<Account, UserInfo>()
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.Role, src => src.Role.ToString());

            // Map MemberProfile → BriefMemberProfileResponse
            config.NewConfig<MemberProfile, BriefMemberProfileResponse>()
                .Map(dest => dest.AccountId, src => src.AccountId)
                .Map(dest => dest.Account, src => src.Account);

            // Map RescuerProfile → BriefRescuerProfileResponse
            config.NewConfig<RescuerProfile, BriefRescuerProfileResponse>()
                .Map(dest => dest.AccountId, src => src.AccountId)
                .Map(dest => dest.Account, src => src.Account);
        }
    }
}