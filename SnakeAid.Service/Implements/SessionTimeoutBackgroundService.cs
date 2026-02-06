using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SnakeAid.Service.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SnakeAid.Service.Implements
{

    /// Background service để monitor và handle session timeouts
    /// Sử dụng scheduled timer approach với in-memory storage cho precision timing without external dependencies
    public class SessionTimeoutBackgroundService : BackgroundService, ISessionTimeoutService
    {
        private readonly ILogger<SessionTimeoutBackgroundService> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        // SortedDictionary để efficiently get earliest timeout
        private readonly SortedDictionary<DateTime, List<Guid>> _timeoutSchedule = new();
        private readonly ConcurrentDictionary<Guid, DateTime> _sessionTimeouts = new();
        private readonly object _scheduleLock = new object();

        // Current timer task cancellation
        private CancellationTokenSource? _currentTimerCancellation;

        // Minimum delay để avoid too frequent checks
        private static readonly TimeSpan MIN_DELAY = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan MAX_DELAY = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan DEFAULT_DELAY = TimeSpan.FromSeconds(30);

        public SessionTimeoutBackgroundService(
            ILogger<SessionTimeoutBackgroundService> logger,
            IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SessionTimeoutBackgroundService started with scheduled timer approach");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Calculate next optimal delay based on earliest timeout
                    var nextDelay = CalculateNextDelay();

                    _logger.LogDebug("Next timeout check in {Delay}ms. Monitoring {Count} sessions",
                        nextDelay.TotalMilliseconds, _sessionTimeouts.Count);

                    // Wait until next scheduled timeout or cancellation
                    using var timerCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                    _currentTimerCancellation = timerCts;

                    await Task.Delay(nextDelay, timerCts.Token);

                    // Process any expired sessions
                    await ProcessExpiredSessions();
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested or timer is reset
                    if (stoppingToken.IsCancellationRequested)
                        break;
                    // Continue if it was just a timer reset
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in scheduled timer loop: {Message}", ex.Message);
                    // Use default delay on error to avoid tight loops
                    await Task.Delay(DEFAULT_DELAY, stoppingToken);
                }
            }

            _logger.LogInformation("SessionTimeoutBackgroundService stopped");
        }


        /// Add session to monitoring with precise timeout scheduling
        public void ScheduleSessionTimeout(Guid sessionId, DateTime timeoutAt)
        {
            lock (_scheduleLock)
            {
                // Remove existing if present
                if (_sessionTimeouts.TryGetValue(sessionId, out var existingTimeout))
                {
                    RemoveFromSchedule(sessionId, existingTimeout);
                }

                // Add to both collections
                _sessionTimeouts[sessionId] = timeoutAt;

                if (!_timeoutSchedule.ContainsKey(timeoutAt))
                {
                    _timeoutSchedule[timeoutAt] = new List<Guid>();
                }
                _timeoutSchedule[timeoutAt].Add(sessionId);

                _logger.LogDebug("Scheduled precise timeout for session {SessionId} at {TimeoutAt}", sessionId, timeoutAt);
            }

            // Reset timer để reschedule với timeout mới
            RescheduleTimer();
        }


        /// Remove session from monitoring (when session completed/cancelled before timeout)
        public void CancelSessionTimeout(Guid sessionId)
        {
            lock (_scheduleLock)
            {
                if (_sessionTimeouts.TryRemove(sessionId, out var timeoutAt))
                {
                    RemoveFromSchedule(sessionId, timeoutAt);
                    _logger.LogDebug("Cancelled timeout monitoring for session {SessionId} (was scheduled for {TimeoutAt})", sessionId, timeoutAt);
                }
            }

            // Reset timer nếu có changes
            RescheduleTimer();
        }


        /// Process sessions that have timed out (more efficient with scheduled approach)
        private async Task ProcessExpiredSessions()
        {
            var currentTime = DateTime.UtcNow;
            var expiredSessions = new List<Guid>();
            var expiredTimeSlots = new List<DateTime>();

            lock (_scheduleLock)
            {
                // Get all time slots that have expired
                foreach (var timeSlot in _timeoutSchedule.Keys.ToList())
                {
                    if (currentTime >= timeSlot)
                    {
                        expiredSessions.AddRange(_timeoutSchedule[timeSlot]);
                        expiredTimeSlots.Add(timeSlot);
                    }
                    else
                    {
                        break; // SortedDictionary is ordered, so we can break early
                    }
                }

                // Cleanup expired time slots
                foreach (var timeSlot in expiredTimeSlots)
                {
                    _timeoutSchedule.Remove(timeSlot);
                }

                // Remove from session tracking
                foreach (var sessionId in expiredSessions)
                {
                    _sessionTimeouts.TryRemove(sessionId, out _);
                }
            }

            if (!expiredSessions.Any())
            {
                return; // No expired sessions
            }

            _logger.LogInformation("Processing {Count} expired sessions", expiredSessions.Count);

            // Process each expired session
            using var scope = _serviceScopeFactory.CreateScope();
            var sessionService = scope.ServiceProvider.GetRequiredService<IRescueRequestSessionService>();

            foreach (var sessionId in expiredSessions)
            {
                try
                {
                    // Check if session was rescheduled after we selected it
                    if (_sessionTimeouts.TryGetValue(sessionId, out var newTimeout) && newTimeout > currentTime)
                    {
                        _logger.LogDebug("Session {SessionId} was rescheduled to {NewTimeout}; skipping timeout processing",
                            sessionId, newTimeout);
                        continue;
                    }

                    // Handle the session timeout (includes expanding to new session if possible)
                    await sessionService.HandleSessionTimeoutAsync(sessionId);

                    _logger.LogInformation("Successfully processed timeout for session {SessionId}", sessionId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error handling timeout for session {SessionId}: {Message}", sessionId, ex.Message);
                    // Continue processing other sessions even if one fails
                }
            }
        }



        /// Get current queue status for monitoring/debugging
        public (int TotalSessions, int ExpiredCount, int PendingCount) GetQueueStatus()
        {
            var currentTime = DateTime.UtcNow;
            var total = _sessionTimeouts.Count;
            var expired = _sessionTimeouts.Count(x => currentTime >= x.Value);
            var pending = total - expired;

            return (total, expired, pending);
        }


        /// Calculate optimal delay until next timeout check
        private TimeSpan CalculateNextDelay()
        {
            lock (_scheduleLock)
            {
                if (_timeoutSchedule.Count == 0)
                {
                    // No sessions scheduled, use default delay
                    return DEFAULT_DELAY;
                }

                // Get earliest timeout
                var earliestTimeout = _timeoutSchedule.Keys.First();
                var now = DateTime.UtcNow;

                if (earliestTimeout <= now)
                {
                    // Already have expired sessions, process immediately
                    return MIN_DELAY;
                }

                var calculatedDelay = earliestTimeout - now;

                // Clamp between min and max delays
                if (calculatedDelay < MIN_DELAY)
                    return MIN_DELAY;
                if (calculatedDelay > MAX_DELAY)
                    return MAX_DELAY;

                return calculatedDelay;
            }
        }


        /// Reset current timer to reschedule with new timeout
        private void RescheduleTimer()
        {
            try
            {
                // Cancel current timer to trigger reschedule
                _currentTimerCancellation?.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // Ignore if already disposed
            }
        }


        /// Remove session from schedule collections
        private void RemoveFromSchedule(Guid sessionId, DateTime timeoutAt)
        {
            if (_timeoutSchedule.TryGetValue(timeoutAt, out var sessionList))
            {
                sessionList.Remove(sessionId);
                if (sessionList.Count == 0)
                {
                    _timeoutSchedule.Remove(timeoutAt);
                }
            }
        }


        /// For health checks - ensures service is running properly
        public bool IsHealthy()
        {
            // Simple health check - service should be able to process the queue
            return _sessionTimeouts.Count < 1000; // Reasonable limit
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("SessionTimeoutBackgroundService is stopping...");
            await base.StopAsync(cancellationToken);
        }
    }
}