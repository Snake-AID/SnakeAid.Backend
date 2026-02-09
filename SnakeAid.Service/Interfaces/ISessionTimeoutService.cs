using System;
using System.Threading.Tasks;

namespace SnakeAid.Service.Interfaces
{

    public interface ISessionTimeoutService
    {

        /// Add session to monitoring queue with timeout
        void ScheduleSessionTimeout(Guid sessionId, DateTime timeoutAt);


        /// Remove session from monitoring (when session completed/cancelled)
        void CancelSessionTimeout(Guid sessionId);


        /// Get current queue status for monitoring
        (int TotalSessions, int ExpiredCount, int PendingCount) GetQueueStatus();

        /// Get detailed monitoring info for all tracked sessions
        List<SessionMonitorInfo> GetMonitoringInfo();

        /// Health check for the service
        bool IsHealthy();
    }

    public class SessionMonitorInfo
    {
        public Guid SessionId { get; set; }
        public DateTime TimeoutAt { get; set; }
        public TimeSpan TimeRemaining { get; set; }
        public bool IsExpired { get; set; }
    }
}