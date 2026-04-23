using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.User;
using SnakeAid.Core.Responses.User;

namespace SnakeAid.Service.Interfaces
{
    public interface IAdminUserService
    {
        /// <summary>
        /// Get paginated list of users with optional filters
        /// </summary>
        Task<PagedData<AdminUserSummaryResponse>> GetAdminUsersAsync(
            string? role = null,
            bool? isActive = null,
            string? searchTerm = null,
            int page = 1,
            int pageSize = 50);

        /// <summary>
        /// Get detailed user information
        /// </summary>
        Task<AdminUserDetailResponse> GetAdminUserDetailAsync(Guid userId);

        /// <summary>
        /// Create a new rescuer account by an admin
        /// </summary>
        Task<AdminUserDetailResponse> CreateRescuerAsync(AdminCreateRescuerRequest request);

        /// <summary>
        /// Ban/deactivate a user account
        /// </summary>
        Task<AdminUserDetailResponse> BanUserAsync(Guid userId, string reason);

        /// <summary>
        /// Unban/reactivate a user account
        /// </summary>
        Task<AdminUserDetailResponse> UnbanUserAsync(Guid userId);
    }
}
