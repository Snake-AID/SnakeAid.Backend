using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Domains;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace SnakeAid.Service.Hubs
{
    [Authorize]
    public class ConsultationHub : Hub
    {
        private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
        private readonly ILogger<ConsultationHub> _logger;

        // Simple in-memory rate limiting: user -> (minute timestamp -> message count)
        private static readonly ConcurrentDictionary<Guid, ConcurrentDictionary<int, int>> _messageCounts = new();

        private const int MaxMessagesPerMinute = 10;

        public ConsultationHub(
            IUnitOfWork<SnakeAidDbContext> unitOfWork,
            ILogger<ConsultationHub> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            try
            {
                var consultationId = GetConsultationIdFromQuery();
                var currentUserId = GetCurrentUserId();

                // Validate consultation exists and user has access
                var consultation = await _unitOfWork.GetRepository<Consultation>()
                    .FirstOrDefaultAsync(
                        predicate: c => c.Id == consultationId,
                        include: query => query.Include(c => c.Caller).Include(c => c.Callee));

                if (consultation == null)
                {
                    _logger.LogWarning("Consultation {ConsultationId} not found for user {UserId}", consultationId, currentUserId);
                    Context.Abort();
                    return;
                }

                // Check if user is Caller or Callee
                if (consultation.CallerId != currentUserId && consultation.CalleeId != currentUserId)
                {
                    _logger.LogWarning("User {UserId} attempted to access consultation {ConsultationId} without permission", currentUserId, consultationId);
                    Context.Abort();
                    return;
                }

                // Join consultation group
                var groupName = GetConsultationGroupName(consultationId);
                await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

                _logger.LogInformation("User {UserId} connected to consultation {ConsultationId} in group {GroupName}",
                    currentUserId, consultationId, groupName);

                await base.OnConnectedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during consultation hub connection for user {UserId}", GetCurrentUserId());
                Context.Abort();
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                var consultationId = GetConsultationIdFromQuery();
                var groupName = GetConsultationGroupName(consultationId);
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

                _logger.LogInformation("User {UserId} disconnected from consultation {ConsultationId}",
                    GetCurrentUserId(), consultationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during consultation hub disconnection for user {UserId}", GetCurrentUserId());
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task ReceiveMessage(string content, string? attachmentUrl = null)
        {
            try
            {
                var consultationId = GetConsultationIdFromQuery();
                var currentUserId = GetCurrentUserId();

                // Rate limiting check (10 messages per minute)
                if (!CheckRateLimit(currentUserId))
                {
                    throw new HubException("Rate limit exceeded. Maximum 10 messages per minute.");
                }

                if (string.IsNullOrWhiteSpace(content) && string.IsNullOrWhiteSpace(attachmentUrl))
                {
                    throw new HubException("Message must contain content or attachment");
                }

                // Create and save chat message
                var chatMessage = new ChatMessage
                {
                    Id = Guid.NewGuid(),
                    ConsultationId = consultationId,
                    SenderId = currentUserId,
                    Content = content?.Trim(),
                    AttachmentUrl = attachmentUrl,
                    SentAt = DateTime.UtcNow
                };

                var messageRepo = _unitOfWork.GetRepository<ChatMessage>();
                await messageRepo.InsertAsync(chatMessage);
                await _unitOfWork.CommitAsync();

                _logger.LogInformation("Chat message saved: {MessageId} in consultation {ConsultationId} by user {UserId}",
                    chatMessage.Id, consultationId, currentUserId);

                // Broadcast to consultation group
                var groupName = GetConsultationGroupName(consultationId);
                await Clients.Group(groupName).SendAsync("ReceiveMessage", new
                {
                    Id = chatMessage.Id,
                    Content = chatMessage.Content,
                    AttachmentUrl = chatMessage.AttachmentUrl,
                    SenderId = chatMessage.SenderId,
                    SentAt = chatMessage.SentAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error receiving message from user {UserId}", GetCurrentUserId());
                throw new HubException("Failed to send message");
            }
        }

        public async Task Signal(string eventType, string payload)
        {
            try
            {
                var consultationId = GetConsultationIdFromQuery();
                var currentUserId = GetCurrentUserId();

                _logger.LogInformation("Signal received: {EventType} from user {UserId} in consultation {ConsultationId}",
                    eventType, currentUserId, consultationId);

                // Broadcast volatile UI state to consultation group (no DB persistence)
                var groupName = GetConsultationGroupName(consultationId);
                await Clients.Group(groupName).SendAsync("Signal", new
                {
                    EventType = eventType,
                    Payload = payload,
                    SenderId = currentUserId,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing signal from user {UserId}", GetCurrentUserId());
                throw new HubException("Failed to send signal");
            }
        }

        private Guid GetConsultationIdFromQuery()
        {
            var consultationIdParam = Context.GetHttpContext()?.Request.Query["consultationId"].ToString();
            if (string.IsNullOrWhiteSpace(consultationIdParam) || !Guid.TryParse(consultationIdParam, out var consultationId))
            {
                throw new HubException("Valid consultationId is required in query string");
            }
            return consultationId;
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new HubException("User identity is missing from token");
            }
            return userId;
        }

        private static string GetConsultationGroupName(Guid consultationId)
        {
            return $"consultation:{consultationId}";
        }

        private bool CheckRateLimit(Guid userId)
        {
            var currentMinute = (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMinutes;

            var userCounts = _messageCounts.GetOrAdd(userId, _ => new ConcurrentDictionary<int, int>());

            // Clean up old entries (older than current minute)
            var keysToRemove = userCounts.Keys.Where(k => k < currentMinute).ToList();
            foreach (var key in keysToRemove)
            {
                userCounts.TryRemove(key, out _);
            }

            // Get current minute count
            var currentCount = userCounts.GetOrAdd(currentMinute, 0);

            if (currentCount >= MaxMessagesPerMinute)
            {
                _logger.LogWarning("Rate limit exceeded for user {UserId}: {CurrentCount}/{MaxCount} messages in minute {Minute}",
                    userId, currentCount, MaxMessagesPerMinute, currentMinute);
                return false;
            }

            // Increment count
            userCounts[currentMinute] = currentCount + 1;
            return true;
        }
    }
}