using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Meta;
using SnakeAid.Service.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SnakeAid.Api.Controllers
{
    [Route("api/monitoring")]
    [ApiController]
    [Authorize] // Only authenticated users can access monitoring
    public class MonitoringController : ControllerBase
    {
        private readonly ISessionTimeoutService _timeoutService;
        private readonly ILogger<MonitoringController> _logger;

        public MonitoringController(
            ISessionTimeoutService timeoutService,
            ILogger<MonitoringController> logger)
        {
            _timeoutService = timeoutService;
            _logger = logger;
        }


        /// Get session timeout service status
        [HttpGet("session-timeout-status")]
        [SwaggerOperation(Summary = "Session Timeout Status", Description = "Get current status of session timeout monitoring service")]
        [SwaggerResponse(200, "Service status retrieved", typeof(ApiResponse<object>))]
        public IActionResult GetSessionTimeoutStatus()
        {
            try
            {
                var (totalSessions, expiredCount, pendingCount) = _timeoutService.GetQueueStatus();
                var isHealthy = _timeoutService.IsHealthy();

                var status = new
                {
                    IsHealthy = isHealthy,
                    TotalSessionsMonitored = totalSessions,
                    ExpiredSessionsInQueue = expiredCount,
                    PendingSessionsInQueue = pendingCount,
                    CheckedAt = DateTime.UtcNow,
                    ServiceStatus = isHealthy ? "Healthy" : "Unhealthy"
                };

                var response = ApiResponseBuilder.BuildSuccessResponse(status, "Session timeout service status retrieved");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving session timeout status: {Message}", ex.Message);
                var response = ApiResponseBuilder.BuildErrorResponse("Failed to retrieve session timeout status");
                return StatusCode(500, response);
            }
        }


        /// Health check endpoint for session timeout service
        [HttpGet("health/session-timeout")]
        [SwaggerOperation(Summary = "Session Timeout Health Check", Description = "Simple health check for session timeout monitoring")]
        [SwaggerResponse(200, "Service is healthy")]
        [SwaggerResponse(503, "Service is unhealthy")]
        public IActionResult SessionTimeoutHealthCheck()
        {
            try
            {
                var isHealthy = _timeoutService.IsHealthy();

                if (isHealthy)
                {
                    return Ok(new { Status = "Healthy", CheckedAt = DateTime.UtcNow });
                }
                else
                {
                    return StatusCode(503, new { Status = "Unhealthy", CheckedAt = DateTime.UtcNow });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Session timeout health check failed: {Message}", ex.Message);
                return StatusCode(503, new { Status = "Unhealthy", Error = ex.Message, CheckedAt = DateTime.UtcNow });
            }
        }
    }
}