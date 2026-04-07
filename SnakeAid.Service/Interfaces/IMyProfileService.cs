using SnakeAid.Core.Requests.MyProfile;
using SnakeAid.Core.Responses.MyProfile;

namespace SnakeAid.Service.Interfaces;

public interface IMyProfileService
{
    Task<MemberMyProfileResponse> GetMemberProfileAsync(Guid accountId);
    Task<MemberMyProfileResponse> UpdateMemberProfileAsync(Guid accountId, UpdateMemberProfileRequest request);
    Task<ExpertMyProfileResponse> GetExpertProfileAsync(Guid accountId);
    Task<ExpertMyProfileResponse> UpdateExpertProfileAsync(Guid accountId, UpdateExpertProfileRequest request);
    Task<RescuerMyProfileResponse> GetRescuerProfileAsync(Guid accountId);
    Task<RescuerMyProfileResponse> UpdateRescuerProfileAsync(Guid accountId, UpdateRescuerProfileRequest request);
}
