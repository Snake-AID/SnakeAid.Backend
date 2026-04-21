using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SnakeAid.Api.Services;
using SnakeAid.Core.Domains;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SnakeAid.Api.Hubs
{
    [Authorize(Roles = "Expert,User")]
    public class ExpertHub : Hub
    {
        private const string ConsultationMembersGroup = "ConsultationMembers";
        // Hardcoded safety switch:
        // OFF by default to avoid unintended cross-environment DB healing when environments share one database.
        private static readonly bool EnablePresenceSelfHealing = false;

        private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
        private readonly IExpertOnlineStatusService _expertOnlineStatusService;
        private readonly ILogger<ExpertHub> _logger;

        public ExpertHub(
            IUnitOfWork<SnakeAidDbContext> unitOfWork,
            IExpertOnlineStatusService expertOnlineStatusService,
            ILogger<ExpertHub> logger)
        {
            _unitOfWork = unitOfWork;
            _expertOnlineStatusService = expertOnlineStatusService;
            _logger = logger;
        }

        public async Task JoinAsExpert()
        {
            EnsureRoleOrThrow("Expert");
            var expertId = GetCurrentExpertId();

            SignalRExpertEmergencyNotificationService.AddConnection(expertId.ToString(), Context.ConnectionId);
            var statusChanged = await _expertOnlineStatusService.SetOnlineAsync(expertId.ToString());
            if (statusChanged)
            {
                await BroadcastExpertPresenceChangedAsync(expertId, true);
            }

            _logger.LogInformation(
                "Expert {ExpertId} joined ExpertHub with connection {ConnectionId}",
                expertId,
                Context.ConnectionId);

            await Clients.Caller.SendAsync("JoinedAsExpert", new
            {
                ExpertId = expertId,
                ConnectionId = Context.ConnectionId,
                Message = "Expert connected successfully."
            });
        }

        public async Task LeaveAsExpert()
        {
            EnsureRoleOrThrow("Expert");
            var expertId = GetCurrentExpertId();

            var trackedExpertId = SignalRExpertEmergencyNotificationService.FindExpertIdByConnection(Context.ConnectionId);
            if (!string.IsNullOrWhiteSpace(trackedExpertId))
            {
                SignalRExpertEmergencyNotificationService.RemoveConnection(trackedExpertId);
            }

            var statusChanged = await _expertOnlineStatusService.SetOfflineAsync(expertId.ToString());
            if (statusChanged)
            {
                await BroadcastExpertPresenceChangedAsync(expertId, false);
            }

            _logger.LogInformation(
                "Expert {ExpertId} left ExpertHub availability with connection {ConnectionId}",
                expertId,
                Context.ConnectionId);

            await Clients.Caller.SendAsync("LeftAsExpert", new
            {
                ExpertId = expertId,
                ConnectionId = Context.ConnectionId,
                Message = "Expert switched to offline successfully."
            });
        }

        public async Task JoinAsMember()
        {
            EnsureRoleOrThrow("User");

            var memberId = GetCurrentUserId();
            await Groups.AddToGroupAsync(Context.ConnectionId, ConsultationMembersGroup);

            if (EnablePresenceSelfHealing)
            {
                await ReconcilePresenceWithDatabaseAsync();
            }

            var onlineExpertIds = SignalRExpertEmergencyNotificationService.ConnectedExperts.Keys
                .Select(ParseGuidOrNull)
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .ToList();

            await Clients.Caller.SendAsync("OnlineExpertsSnapshot", new
            {
                OnlineExpertIds = onlineExpertIds,
                ServerTimeUtc = DateTime.UtcNow
            });

            _logger.LogInformation(
                "Member {MemberId} joined ExpertHub presence group with connection {ConnectionId}. OnlineExperts={OnlineExpertsCount}",
                memberId,
                Context.ConnectionId,
                onlineExpertIds.Count);
        }

        public async Task JoinEmergencyRequestRoom(Guid requestId)
        {
            EnsureRoleOrThrow("User");

            var memberId = GetCurrentUserId();
            var ping = await _unitOfWork.GetRepository<ConsultationPingRequest>().FirstOrDefaultAsync(
                predicate: p => p.Id == requestId && p.RescuerId == memberId);

            if (ping == null)
            {
                throw new HubException("Emergency request was not found for current user.");
            }

            var groupName = SignalRExpertEmergencyNotificationService.BuildEmergencyRequestGroupName(requestId);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            await Clients.Caller.SendAsync("JoinedEmergencyRequestRoom", new
            {
                RequestId = requestId,
                GroupName = groupName
            });
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var expertIdText = SignalRExpertEmergencyNotificationService.FindExpertIdByConnection(Context.ConnectionId);
            if (!string.IsNullOrWhiteSpace(expertIdText) && Guid.TryParse(expertIdText, out var expertId))
            {
                SignalRExpertEmergencyNotificationService.RemoveConnection(expertIdText);
                var statusChanged = await _expertOnlineStatusService.SetOfflineAsync(expertIdText);
                if (statusChanged)
                {
                    await BroadcastExpertPresenceChangedAsync(expertId, false);
                }

                _logger.LogInformation(
                    "Expert {ExpertId} disconnected from ExpertHub. ConnectionId={ConnectionId}",
                    expertId,
                    Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new HubException("Expert identity is missing from token.");
            }

            return userId;
        }

        private Guid GetCurrentExpertId()
        {
            return GetCurrentUserId();
        }

        private void EnsureRoleOrThrow(string role)
        {
            if (Context.User?.IsInRole(role) != true)
            {
                throw new HubException($"This action requires role: {role}.");
            }
        }

        private Task BroadcastExpertPresenceChangedAsync(Guid expertId, bool isOnline)
        {
            return Clients.Group(ConsultationMembersGroup).SendAsync("ExpertPresenceChanged", new
            {
                ExpertId = expertId,
                IsOnline = isOnline,
                ChangedAtUtc = DateTime.UtcNow
            });
        }

        private async Task ReconcilePresenceWithDatabaseAsync()
        {
            var connectedExpertIds = SignalRExpertEmergencyNotificationService.ConnectedExperts.Keys
                .Select(ParseGuidOrNull)
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .ToHashSet();

            var profileRepo = _unitOfWork.GetRepository<ExpertProfile>();
            var profiles = await profileRepo.GetListAsync(predicate: p => p.Account.IsActive, asNoTracking: false);

            var hasChanges = false;
            var updatedCount = 0;
            foreach (var profile in profiles)
            {
                var shouldBeOnline = connectedExpertIds.Contains(profile.AccountId);
                if (profile.IsOnline == shouldBeOnline)
                {
                    continue;
                }

                profile.IsOnline = shouldBeOnline;
                profileRepo.Update(profile);
                hasChanges = true;
                updatedCount++;
            }

            if (hasChanges)
            {
                await _unitOfWork.CommitAsync();
                _logger.LogWarning(
                    "Presence self-healing reconciled ExpertProfile.IsOnline from SignalR memory. UpdatedProfiles={UpdatedCount}",
                    updatedCount);
            }
        }

        private static Guid? ParseGuidOrNull(string value)
        {
            return Guid.TryParse(value, out var guid) ? guid : null;
        }
    }
}
