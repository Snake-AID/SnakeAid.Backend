using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SnakeAid.Api.Hubs;
using SnakeAid.Core.Exceptions;
using SnakeAid.Service.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Api.Services
{
    public class SignalRExpertEmergencyNotificationService : IExpertEmergencyNotificationService
    {
        private readonly IHubContext<ExpertHub> _hubContext;
        private readonly ILogger<SignalRExpertEmergencyNotificationService> _logger;

        public static ConcurrentDictionary<string, string> ConnectedExperts { get; } = new();

        public SignalRExpertEmergencyNotificationService(
            IHubContext<ExpertHub> hubContext,
            ILogger<SignalRExpertEmergencyNotificationService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public bool IsExpertConnected(string expertId)
        {
            return ConnectedExperts.ContainsKey(expertId);
        }

        public async Task SendEmergencyRequestAsync(string expertId, object requestData)
        {
            if (!ConnectedExperts.TryGetValue(expertId, out var connectionId))
            {
                _logger.LogWarning("Expert {ExpertId} is not connected. Skip emergency request push.", expertId);
                return;
            }

            await SafeExecuteAsync(
                () => _hubContext.Clients.Client(connectionId).SendAsync("EmergencyConsultationRequest", requestData),
                "SendEmergencyRequest",
                expertId);
        }

        public static void AddConnection(string expertId, string connectionId)
        {
            ConnectedExperts[expertId] = connectionId;
        }

        public static bool RemoveConnection(string expertId)
        {
            return ConnectedExperts.TryRemove(expertId, out _);
        }

        public static string? FindExpertIdByConnection(string connectionId)
        {
            var item = ConnectedExperts.FirstOrDefault(x => x.Value == connectionId);
            return item.Equals(default(KeyValuePair<string, string>)) ? null : item.Key;
        }

        private async Task SafeExecuteAsync(Func<Task> action, string actionName, string expertId)
        {
            try
            {
                await action();
                _logger.LogInformation("Executed {Action} for expert {ExpertId}", actionName, expertId);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(new SignalRNotificationException($"Error in {actionName} for expert {expertId}", ex), "SignalR_Notification_Error");
            }
        }
    }
}
