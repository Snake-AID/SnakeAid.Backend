using Microsoft.Extensions.Logging;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Requests.MyProfile;
using SnakeAid.Core.Responses.MyProfile;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Service.Implements;

public class MyProfileService : IMyProfileService
{
    private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
    private readonly ILogger<MyProfileService> _logger;

    public MyProfileService(IUnitOfWork<SnakeAidDbContext> unitOfWork, ILogger<MyProfileService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<MemberMyProfileResponse> GetMemberProfileAsync(Guid accountId)
    {
        var account = await GetAccountAsync(accountId, AccountRole.User);
        var profile = await GetMemberProfileEntityAsync(accountId);
        if (profile == null)
        {
            throw new NotFoundException("Member profile not found.");
        }

        return MapMember(account, profile);
    }

    public async Task<MemberMyProfileResponse> UpdateMemberProfileAsync(Guid accountId, UpdateMemberProfileRequest request)
    {
        var account = await GetAccountAsync(accountId, AccountRole.User, asNoTracking: false);
        var profile = await GetMemberProfileEntityAsync(accountId, asNoTracking: false);
        if (profile == null)
        {
            throw new NotFoundException("Member profile not found.");
        }

        UpdateAccountFields(account, request.FullName, request.PhoneNumber, request.AvatarUrl);
        profile.EmergencyContacts = request.EmergencyContacts ?? new List<string>();
        profile.HasUnderlyingDisease = request.HasUnderlyingDisease;

        await _unitOfWork.CommitAsync();
        _logger.LogInformation("Updated member profile for account {AccountId}.", accountId);

        return MapMember(account, profile);
    }

    public async Task<ExpertMyProfileResponse> GetExpertProfileAsync(Guid accountId)
    {
        var account = await GetAccountAsync(accountId, AccountRole.Expert);
        var profile = await GetExpertProfileEntityAsync(accountId);
        if (profile == null)
        {
            throw new NotFoundException("Expert profile not found.");
        }

        return MapExpert(account, profile);
    }

    public async Task<ExpertMyProfileResponse> UpdateExpertProfileAsync(Guid accountId, UpdateExpertProfileRequest request)
    {
        var account = await GetAccountAsync(accountId, AccountRole.Expert, asNoTracking: false);
        var profile = await GetExpertProfileEntityAsync(accountId, asNoTracking: false);
        if (profile == null)
        {
            throw new NotFoundException("Expert profile not found.");
        }

        UpdateAccountFields(account, request.FullName, request.PhoneNumber, request.AvatarUrl);
        profile.Biography = request.Biography;
        profile.ConsultationFee = request.ScheduledConsultationFee;
        profile.EmergencyConsultationFee = request.EmergencyConsultationFee ?? request.ScheduledConsultationFee;

        await _unitOfWork.CommitAsync();
        _logger.LogInformation("Updated expert profile for account {AccountId}.", accountId);

        return MapExpert(account, profile);
    }

    public async Task<RescuerMyProfileResponse> GetRescuerProfileAsync(Guid accountId)
    {
        var account = await GetAccountAsync(accountId, AccountRole.Rescuer);
        var profile = await GetRescuerProfileEntityAsync(accountId);
        if (profile == null)
        {
            throw new NotFoundException("Rescuer profile not found.");
        }

        return MapRescuer(account, profile);
    }

    public async Task<RescuerMyProfileResponse> UpdateRescuerProfileAsync(Guid accountId, UpdateRescuerProfileRequest request)
    {
        var account = await GetAccountAsync(accountId, AccountRole.Rescuer, asNoTracking: false);
        var profile = await GetRescuerProfileEntityAsync(accountId, asNoTracking: false);
        if (profile == null)
        {
            throw new NotFoundException("Rescuer profile not found.");
        }

        UpdateAccountFields(account, request.FullName, request.PhoneNumber, request.AvatarUrl);

        await _unitOfWork.CommitAsync();
        _logger.LogInformation("Updated rescuer profile for account {AccountId}.", accountId);

        return MapRescuer(account, profile);
    }

    private async Task<Account> GetAccountAsync(
        Guid accountId,
        AccountRole expectedRole,
        bool asNoTracking = true)
    {
        var account = await _unitOfWork.GetRepository<Account>().FirstOrDefaultAsync(
            predicate: a => a.Id == accountId,
            asNoTracking: asNoTracking);

        if (account == null)
        {
            throw new NotFoundException("Account not found.");
        }

        if (account.Role != expectedRole)
        {
            throw new ForbiddenException($"Current account is not a {expectedRole} profile.");
        }

        return account;
    }

    private Task<MemberProfile?> GetMemberProfileEntityAsync(Guid accountId, bool asNoTracking = true)
    {
        return _unitOfWork.GetRepository<MemberProfile>().FirstOrDefaultAsync(
            predicate: p => p.AccountId == accountId,
            asNoTracking: asNoTracking);
    }

    private Task<ExpertProfile?> GetExpertProfileEntityAsync(Guid accountId, bool asNoTracking = true)
    {
        return _unitOfWork.GetRepository<ExpertProfile>().FirstOrDefaultAsync(
            predicate: p => p.AccountId == accountId,
            asNoTracking: asNoTracking);
    }

    private Task<RescuerProfile?> GetRescuerProfileEntityAsync(Guid accountId, bool asNoTracking = true)
    {
        return _unitOfWork.GetRepository<RescuerProfile>().FirstOrDefaultAsync(
            predicate: p => p.AccountId == accountId,
            asNoTracking: asNoTracking);
    }

    private static void UpdateAccountFields(Account account, string fullName, string? phoneNumber, string? avatarUrl)
    {
        account.FullName = fullName;
        account.PhoneNumber = phoneNumber;
        account.AvatarUrl = avatarUrl;
        account.UpdatedAt = DateTime.UtcNow;
    }

    private static MemberMyProfileResponse MapMember(Account account, MemberProfile profile)
    {
        return new MemberMyProfileResponse
        {
            AccountId = account.Id,
            UserName = account.UserName ?? string.Empty,
            FullName = account.FullName,
            Email = account.Email,
            PhoneNumber = account.PhoneNumber,
            AvatarUrl = account.AvatarUrl,
            Role = account.Role,
            IsActive = account.IsActive,
            ReputationPoints = account.ReputationPoints,
            ReputationStatus = account.ReputationStatus,
            Rating = profile.Rating,
            RatingCount = profile.RatingCount,
            EmergencyContacts = profile.EmergencyContacts ?? new List<string>(),
            HasUnderlyingDisease = profile.HasUnderlyingDisease,
            CreatedAt = account.CreatedAt,
            UpdatedAt = account.UpdatedAt
        };
    }

    private static ExpertMyProfileResponse MapExpert(Account account, ExpertProfile profile)
    {
        return new ExpertMyProfileResponse
        {
            AccountId = account.Id,
            UserName = account.UserName ?? string.Empty,
            FullName = account.FullName,
            Email = account.Email,
            PhoneNumber = account.PhoneNumber,
            AvatarUrl = account.AvatarUrl,
            Role = account.Role,
            IsActive = account.IsActive,
            ReputationPoints = account.ReputationPoints,
            ReputationStatus = account.ReputationStatus,
            Biography = profile.Biography,
            IsOnline = profile.IsOnline,
            ScheduledConsultationFee = profile.ConsultationFee,
            EmergencyConsultationFee = profile.EmergencyConsultationFee ?? profile.ConsultationFee,
            Rating = profile.Rating,
            RatingCount = profile.RatingCount,
            CreatedAt = account.CreatedAt,
            UpdatedAt = account.UpdatedAt
        };
    }

    private static RescuerMyProfileResponse MapRescuer(Account account, RescuerProfile profile)
    {
        return new RescuerMyProfileResponse
        {
            AccountId = account.Id,
            UserName = account.UserName ?? string.Empty,
            FullName = account.FullName,
            Email = account.Email,
            PhoneNumber = account.PhoneNumber,
            AvatarUrl = account.AvatarUrl,
            Role = account.Role,
            IsActive = account.IsActive,
            ReputationPoints = account.ReputationPoints,
            ReputationStatus = account.ReputationStatus,
            IsOnline = profile.IsOnline,
            IsAvailable = profile.IsAvailable,
            Type = profile.Type,
            Rating = profile.Rating,
            RatingCount = profile.RatingCount,
            TotalMissions = profile.TotalMissions,
            CompletedMissions = profile.CompletedMissions,
            LastLocationUpdate = profile.LastLocationUpdate,
            Latitude = profile.LastLocation?.Y,
            Longitude = profile.LastLocation?.X,
            CreatedAt = account.CreatedAt,
            UpdatedAt = account.UpdatedAt
        };
    }
}
