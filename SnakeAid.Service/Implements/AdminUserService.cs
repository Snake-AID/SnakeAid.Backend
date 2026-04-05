using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Responses.User;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Implements;
using SnakeAid.Service.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Service.Implements
{
    public class AdminUserService : IAdminUserService
    {
        private readonly ILogger<AdminUserService> _logger;
        private readonly UserManager<Account> _userManager;
        private readonly UnitOfWork<SnakeAidDbContext> _unitOfWork;

        public AdminUserService(
            ILogger<AdminUserService> logger,
            UserManager<Account> userManager,
            UnitOfWork<SnakeAidDbContext> unitOfWork)
        {
            _logger = logger;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
        }

        public async Task<PagedData<AdminUserSummaryResponse>> GetAdminUsersAsync(
            string? role = null,
            bool? isActive = null,
            string? searchTerm = null,
            int page = 1,
            int pageSize = 50)
        {
            try
            {
                var repo = _unitOfWork.GetRepository<Account>();

                // Build predicate for filtering
                var hasRoleFilter = !string.IsNullOrEmpty(role);
                var hasActiveFilter = isActive.HasValue;
                var hasSearchTerm = !string.IsNullOrEmpty(searchTerm);

                // Parse role if provided
                AccountRole? roleEnum = null;
                if (hasRoleFilter && Enum.TryParse<AccountRole>(role, true, out var parsedRole))
                {
                    roleEnum = parsedRole;
                }

                return await repo.GetPagingListAsync<AdminUserSummaryResponse>(
                    predicate: u =>
                        (!hasRoleFilter || (roleEnum.HasValue && u.Role == roleEnum.Value)) &&
                        (!hasActiveFilter || u.IsActive == isActive.Value) &&
                        (!hasSearchTerm ||
                            u.UserName!.ToLower().Contains(searchTerm!.ToLower()) ||
                            u.FullName!.ToLower().Contains(searchTerm!.ToLower()) ||
                            u.Email!.ToLower().Contains(searchTerm!.ToLower())),
                    orderBy: q => q.OrderByDescending(u => u.CreatedAt),
                    page: page,
                    size: pageSize,
                    selector: u => u.Adapt<AdminUserSummaryResponse>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving admin users list: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<AdminUserDetailResponse> GetAdminUserDetailAsync(Guid userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());

                if (user == null)
                {
                    throw new NotFoundException($"User with ID {userId} not found.");
                }

                return user.Adapt<AdminUserDetailResponse>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user detail for ID {UserId}: {Message}", userId, ex.Message);
                throw;
            }
        }

        public async Task<AdminUserDetailResponse> BanUserAsync(Guid userId, string reason)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());

                if (user == null)
                {
                    throw new NotFoundException($"User with ID {userId} not found.");
                }

                user.IsActive = false;
                user.SuspendedUntil = null;

                user.SuspensionReason = reason;
                user.UpdatedAt = DateTime.UtcNow;

                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    var errorMessage = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to ban user: {errorMessage}");
                }

                _logger.LogInformation(
                    "User {UserId} has been banned. Reason: {Reason}.",
                    userId,
                    reason);

                return user.Adapt<AdminUserDetailResponse>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error banning user {UserId}: {Message}", userId, ex.Message);
                throw;
            }
        }

        public async Task<AdminUserDetailResponse> UnbanUserAsync(Guid userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());

                if (user == null)
                {
                    throw new NotFoundException($"User with ID {userId} not found.");
                }

                user.IsActive = true;
                user.SuspendedUntil = null;
                user.SuspensionReason = null;
                user.UpdatedAt = DateTime.UtcNow;

                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    var errorMessage = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to unban user: {errorMessage}");
                }

                _logger.LogInformation("User {UserId} has been unbanned.", userId);

                return user.Adapt<AdminUserDetailResponse>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unbanning user {UserId}: {Message}", userId, ex.Message);
                throw;
            }
        }
    }
}
