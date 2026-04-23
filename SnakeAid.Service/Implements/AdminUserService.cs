using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.User;
using SnakeAid.Core.Responses.User;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Implements;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace SnakeAid.Service.Implements
{
    public class AdminUserService : IAdminUserService
    {
        private readonly ILogger<AdminUserService> _logger;
        private readonly UserManager<Account> _userManager;
        private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;

        public AdminUserService(
            ILogger<AdminUserService> logger,
            UserManager<Account> userManager,
            IUnitOfWork<SnakeAidDbContext> unitOfWork)
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
                var activeFilterValue = isActive ?? false;
                var searchTermLower = searchTerm?.ToLower() ?? string.Empty;

                // Parse role if provided
                AccountRole? roleEnum = null;
                if (hasRoleFilter && Enum.TryParse<AccountRole>(role, true, out var parsedRole))
                {
                    roleEnum = parsedRole;
                }

                return await repo.GetPagingListAsync<AdminUserSummaryResponse>(
                    predicate: u =>
                        (!hasRoleFilter || (roleEnum.HasValue && u.Role == roleEnum.Value)) &&
                        (!hasActiveFilter || u.IsActive == activeFilterValue) &&
                        (!hasSearchTerm ||
                            (u.UserName ?? string.Empty).ToLower().Contains(searchTermLower) ||
                            (u.FullName ?? string.Empty).ToLower().Contains(searchTermLower) ||
                            (u.Email ?? string.Empty).ToLower().Contains(searchTermLower)),
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
                var repo = _unitOfWork.GetRepository<Account>();
                var user = await repo.FirstOrDefaultAsync(
                    predicate: u => u.Id == userId,
                    include: q => q
                        .Include(u => u.MemberProfile)
                        .Include(u => u.ExpertProfile)
                        .Include(u => u.RescuerProfile));

                if (user == null)
                {
                    throw new NotFoundException($"User with ID {userId} not found.");
                }

                return new AdminUserDetailResponse
                {
                    Id = user.Id,
                    UserName = user.UserName ?? string.Empty,
                    FullName = user.FullName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Role = user.Role,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt,
                    IsActive = user.IsActive,
                    ReputationPoints = user.ReputationPoints,
                    ReputationStatus = user.ReputationStatus,
                    SuspendedUntil = user.SuspendedUntil,
                    SuspensionReason = user.SuspensionReason,
                    AvatarUrl = user.AvatarUrl,
                    MemberProfile = user.MemberProfile == null
                        ? null
                        : new AdminMemberProfileResponse
                        {
                            Rating = user.MemberProfile.Rating,
                            RatingCount = user.MemberProfile.RatingCount,
                            HasUnderlyingDisease = user.MemberProfile.HasUnderlyingDisease,
                            EmergencyContacts = user.MemberProfile.EmergencyContacts ?? new List<string>()
                        },
                    ExpertProfile = user.ExpertProfile == null
                        ? null
                        : new AdminExpertProfileResponse
                        {
                            Biography = user.ExpertProfile.Biography,
                            IsOnline = user.ExpertProfile.IsOnline,
                            ConsultationFee = user.ExpertProfile.ConsultationFee,
                            EmergencyConsultationFee = user.ExpertProfile.EmergencyConsultationFee,
                            Rating = user.ExpertProfile.Rating,
                            RatingCount = user.ExpertProfile.RatingCount,
                            IsVerified = user.ExpertProfile.IsVerified
                        },
                    RescuerProfile = user.RescuerProfile == null
                        ? null
                        : new AdminRescuerProfileResponse
                        {
                            IsOnline = user.RescuerProfile.IsOnline,
                            IsAvailable = user.RescuerProfile.IsAvailable,
                            Type = user.RescuerProfile.Type,
                            Rating = user.RescuerProfile.Rating,
                            RatingCount = user.RescuerProfile.RatingCount,
                            TotalMissions = user.RescuerProfile.TotalMissions,
                            CompletedMissions = user.RescuerProfile.CompletedMissions,
                            LastLocationUpdate = user.RescuerProfile.LastLocationUpdate
                        }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user detail for ID {UserId}: {Message}", userId, ex.Message);
                throw;
            }
        }

        public async Task<AdminUserDetailResponse> CreateRescuerAsync(AdminCreateRescuerRequest request)
        {
            try
            {
                var existingUser = await _userManager.FindByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    throw new ConflictException("Email is already in use.");
                }

                var user = new Account
                {
                    Id = Guid.NewGuid(),
                    UserName = request.Email,
                    Email = request.Email,
                    FullName = request.FullName,
                    PhoneNumber = request.PhoneNumber,
                    IsActive = true,
                    EmailConfirmed = true,
                    Role = AccountRole.Rescuer,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var createResult = await _userManager.CreateAsync(user, request.Password);
                if (!createResult.Succeeded)
                {
                    var errorMessages = string.Join("; ", createResult.Errors.Select(e => e.Description));
                    throw new BadRequestException($"Rescuer creation failed: {errorMessages}");
                }

                var rescuerRepository = _unitOfWork.GetRepository<RescuerProfile>();
                var rescuer = new RescuerProfile
                {
                    AccountId = user.Id,
                    Type = request.Type,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await rescuerRepository.InsertAsync(rescuer);

                var walletRepository = _unitOfWork.GetRepository<Wallet>();
                var wallet = new Wallet
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Balance = 0m,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await walletRepository.InsertAsync(wallet);

                await _unitOfWork.CommitAsync();

                return new AdminUserDetailResponse
                {
                    Id = user.Id,
                    UserName = user.UserName ?? string.Empty,
                    FullName = user.FullName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Role = user.Role,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt,
                    IsActive = user.IsActive,
                    ReputationPoints = user.ReputationPoints,
                    ReputationStatus = user.ReputationStatus,
                    AvatarUrl = user.AvatarUrl,
                    RescuerProfile = new AdminRescuerProfileResponse
                    {
                        IsOnline = rescuer.IsOnline,
                        IsAvailable = rescuer.IsAvailable,
                        Type = rescuer.Type,
                        Rating = rescuer.Rating,
                        RatingCount = rescuer.RatingCount,
                        TotalMissions = rescuer.TotalMissions,
                        CompletedMissions = rescuer.CompletedMissions,
                        LastLocationUpdate = rescuer.LastLocationUpdate
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating rescuer account: {Message}", ex.Message);
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
