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
                    _logger.LogDebug("[SessionTimeout] Starting new iteration. Current sessions: {Count}, Schedule slots: {Slots}",
                        _sessionTimeouts.Count, _timeoutSchedule.Count);

                    // Calculate next optimal delay based on earliest timeout
                    var nextDelay = CalculateNextDelay();

                    _logger.LogInformation("[SessionTimeout] Next timeout check in {Delay}s ({DelayMs}ms). Monitoring {Count} sessions",
                        Math.Round(nextDelay.TotalSeconds, 2), nextDelay.TotalMilliseconds, _sessionTimeouts.Count);

                    // Wait until next scheduled timeout or cancellation
                    using var timerCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                    _currentTimerCancellation = timerCts;

                    _logger.LogDebug("[SessionTimeout] Waiting for {Delay}ms before next check...", nextDelay.TotalMilliseconds);
                    await Task.Delay(nextDelay, timerCts.Token);

                    _logger.LogDebug("[SessionTimeout] Timer elapsed, checking for expired sessions...");
                    // Process any expired sessions
                    await ProcessExpiredSessions();
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested or timer is reset
                    if (stoppingToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("[SessionTimeout] Stopping token cancelled, exiting loop");
                        break;
                    }
                    _logger.LogDebug("[SessionTimeout] Timer was reset/cancelled, rescheduling...");
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
            var now = DateTime.UtcNow;
            var timeUntilTimeout = timeoutAt - now;

            lock (_scheduleLock)
            {
                // Remove existing if present
                if (_sessionTimeouts.TryGetValue(sessionId, out var existingTimeout))
                {
                    _logger.LogDebug("[SessionTimeout] Rescheduling session {SessionId} from {OldTimeout} to {NewTimeout}",
                        sessionId, existingTimeout, timeoutAt);
                    RemoveFromSchedule(sessionId, existingTimeout);
                }

                // Add to both collections
                _sessionTimeouts[sessionId] = timeoutAt;

                if (!_timeoutSchedule.ContainsKey(timeoutAt))
                {
                    _timeoutSchedule[timeoutAt] = new List<Guid>();
                }
                _timeoutSchedule[timeoutAt].Add(sessionId);

                _logger.LogInformation("[SessionTimeout] ✅ Scheduled timeout for session {SessionId} at {TimeoutAt} (in {Minutes}m {Seconds}s). Total sessions: {Total}",
                    sessionId, timeoutAt, (int)timeUntilTimeout.TotalMinutes, (int)timeUntilTimeout.Seconds % 60, _sessionTimeouts.Count);
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
                    _logger.LogInformation("[SessionTimeout] ❌ Cancelled timeout monitoring for session {SessionId} (was scheduled for {TimeoutAt}). Remaining sessions: {Count}",
                        sessionId, timeoutAt, _sessionTimeouts.Count);
                }
                else
                {
                    _logger.LogDebug("[SessionTimeout] Attempted to cancel session {SessionId} but it was not found in monitoring", sessionId);
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
                _logger.LogDebug("[SessionTimeout] Checking for expired sessions at {CurrentTime}. Total schedule slots: {Slots}",
                    currentTime, _timeoutSchedule.Count);

                // Get all time slots that have expired
                foreach (var timeSlot in _timeoutSchedule.Keys.ToList())
                {
                    if (currentTime >= timeSlot)
                    {
                        var sessionsInSlot = _timeoutSchedule[timeSlot];
                        _logger.LogDebug("[SessionTimeout] Time slot {TimeSlot} has expired with {Count} sessions",
                            timeSlot, sessionsInSlot.Count);
                        expiredSessions.AddRange(sessionsInSlot);
                        expiredTimeSlots.Add(timeSlot);
                    }
                    else
                    {
                        _logger.LogDebug("[SessionTimeout] Next time slot {TimeSlot} has not expired yet (in {Seconds}s)",
                            timeSlot, (timeSlot - currentTime).TotalSeconds);
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
                _logger.LogDebug("[SessionTimeout] No expired sessions found at this check");
                return; // No expired sessions
            }

            _logger.LogInformation("[SessionTimeout] ⏰ Processing {Count} expired sessions: {SessionIds}",
                expiredSessions.Count, string.Join(", ", expiredSessions));

            // Process each expired session
            using var scope = _serviceScopeFactory.CreateScope();
            var sessionService = scope.ServiceProvider.GetRequiredService<IRescueRequestSessionService>();

            var successCount = 0;
            var errorCount = 0;
            var skippedCount = 0;

            foreach (var sessionId in expiredSessions)
            {
                try
                {
                    _logger.LogDebug("[SessionTimeout] Processing expired session {SessionId}...", sessionId);

                    // Check if session was rescheduled after we selected it
                    if (_sessionTimeouts.TryGetValue(sessionId, out var newTimeout) && newTimeout > currentTime)
                    {
                        _logger.LogInformation("[SessionTimeout] ⚠️ Session {SessionId} was rescheduled to {NewTimeout}; skipping timeout processing",
                            sessionId, newTimeout);
                        skippedCount++;
                        continue;
                    }

                    // Handle the session timeout (includes expanding to new session if possible)
                    await sessionService.HandleSessionTimeoutAsync(sessionId);

                    _logger.LogInformation("[SessionTimeout] ✅ Successfully processed timeout for session {SessionId}", sessionId);
                    successCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[SessionTimeout] ❌ Error handling timeout for session {SessionId}: {Message}", sessionId, ex.Message);
                    errorCount++;
                    // Continue processing other sessions even if one fails
                }
            }

            _logger.LogInformation("[SessionTimeout] Completed processing expired sessions. Success: {Success}, Errors: {Errors}, Skipped: {Skipped}",
                successCount, errorCount, skippedCount);
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
                    _logger.LogDebug("[SessionTimeout] No sessions in schedule, using default delay: {Delay}s", DEFAULT_DELAY.TotalSeconds);
                    // No sessions scheduled, use default delay
                    return DEFAULT_DELAY;
                }

                // Get earliest timeout
                var earliestTimeout = _timeoutSchedule.Keys.First();
                var now = DateTime.UtcNow;

                if (earliestTimeout <= now)
                {
                    _logger.LogDebug("[SessionTimeout] ⚡ Earliest timeout {EarliestTimeout} has already passed! Processing immediately with min delay",
                        earliestTimeout);
                    // Already have expired sessions, process immediately
                    return MIN_DELAY;
                }

                var calculatedDelay = earliestTimeout - now;

                // Clamp between min and max delays
                if (calculatedDelay < MIN_DELAY)
                {
                    _logger.LogDebug("[SessionTimeout] Calculated delay {Delay}ms too small, using MIN_DELAY", calculatedDelay.TotalMilliseconds);
                    return MIN_DELAY;
                }
                if (calculatedDelay > MAX_DELAY)
                {
                    _logger.LogDebug("[SessionTimeout] Calculated delay {Delay}s too large, using MAX_DELAY", calculatedDelay.TotalSeconds);
                    return MAX_DELAY;
                }

                _logger.LogDebug("[SessionTimeout] ⏱️ Next timeout at {EarliestTimeout}, delay: {DelaySeconds}s ({DelayMs}ms)",
                    earliestTimeout, Math.Round(calculatedDelay.TotalSeconds, 2), calculatedDelay.TotalMilliseconds);

                return calculatedDelay;
            }
        }


        /// Reset current timer to reschedule with new timeout
        private void RescheduleTimer()
        {
            try
            {
                _logger.LogDebug("[SessionTimeout] 🔄 Rescheduling timer due to schedule change");
                // Cancel current timer to trigger reschedule
                _currentTimerCancellation?.Cancel();
            }
            catch (ObjectDisposedException)
            {
                _logger.LogDebug("[SessionTimeout] Timer cancellation already disposed, ignoring");
                // Ignore if already disposed
            }
        }


        /// Remove session from schedule collections
        private void RemoveFromSchedule(Guid sessionId, DateTime timeoutAt)
        {
            if (_timeoutSchedule.TryGetValue(timeoutAt, out var sessionList))
            {
                sessionList.Remove(sessionId);
                _logger.LogDebug("[SessionTimeout] Removed session {SessionId} from schedule at {TimeoutAt}. Remaining in slot: {Count}",
                    sessionId, timeoutAt, sessionList.Count);
                if (sessionList.Count == 0)
                {
                    _timeoutSchedule.Remove(timeoutAt);
                    _logger.LogDebug("[SessionTimeout] Removed empty time slot {TimeoutAt}", timeoutAt);
                }
            }
            else
            {
                _logger.LogDebug("[SessionTimeout] Time slot {TimeoutAt} not found when trying to remove session {SessionId}",
                    timeoutAt, sessionId);
            }
        }


        /// For health checks - ensures service is running properly
        public bool IsHealthy()
        {
            // Simple health check - service should be able to process the queue
            return _sessionTimeouts.Count < 1000; // Reasonable limit
        }

        /// Get detailed monitoring info for all tracked sessions
        public List<SessionMonitorInfo> GetMonitoringInfo()
        {
            lock (_scheduleLock)
            {
                var result = new List<SessionMonitorInfo>();
                var now = DateTime.UtcNow;

                foreach (var kvp in _sessionTimeouts)
                {
                    var sessionId = kvp.Key;
                    var timeoutAt = kvp.Value;
                    var timeRemaining = timeoutAt - now;
                    var isExpired = timeRemaining < TimeSpan.Zero;

                    result.Add(new SessionMonitorInfo
                    {
                        SessionId = sessionId,
                        TimeoutAt = timeoutAt,
                        TimeRemaining = timeRemaining,
                        IsExpired = isExpired
                    });
                }

                return result.OrderBy(s => s.TimeoutAt).ToList();
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("SessionTimeoutBackgroundService is stopping...");
            await base.StopAsync(cancellationToken);
        }
    }
}