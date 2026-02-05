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


        /// Health check for the service
        bool IsHealthy();
    }
}