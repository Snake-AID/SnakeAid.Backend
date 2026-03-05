using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.Expert;
using SnakeAid.Core.Responses.Expert;
using SnakeAid.Core.Responses.UserFeedback;
using SnakeAid.Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SnakeAid.Api.Controllers
{
    [ApiController]
    [Route("api/v1/experts")]
    public class ExpertController : ControllerBase
    {
        private readonly IExpertService _expertService;

        public ExpertController(IExpertService expertService)
        {
            _expertService = expertService;
        }

        /// <summary>
        /// Expert configures profile settings (Biography, Fee).
        /// </summary>
        [HttpPut("me/settings")]
        [Authorize(Roles = "Expert")]
        public async Task<IActionResult> UpdateSettings([FromBody] ExpertSettingsRequest request)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdStr, out var expertId)) return Unauthorized();

            await _expertService.UpdateSettingsAsync(expertId, request);
            return Ok(new { message = "Settings updated successfully" });
        }

        /// <summary>
        /// Expert generates time slots for the week based on configurable time blocks.
        /// </summary>
        [HttpPost("me/time-slots/bulk")]
        [Authorize(Roles = "Expert")]
        public async Task<IActionResult> CreateBulkTimeSlots([FromBody] BulkTimeSlotRequest request)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdStr, out var expertId)) return Unauthorized();

            await _expertService.CreateBulkTimeSlotsAsync(expertId, request);
            return Ok(new { message = "Time slots generated successfully" });
        }

        /// <summary>
        /// List all experts (for patients to browse).
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PagingResponse<ExpertProfileResponse>>> GetExperts([FromQuery] PaginationRequest request)
        {
            var result = await _expertService.GetExpertsAsync(request);
            return Ok(result);
        }

        /// <summary>
        /// Get detailed profile for a specific expert.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ExpertProfileResponse>> GetExpertProfile(Guid id)
        {
            var result = await _expertService.GetExpertProfileAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// Get expert's available time slots (for the immediate week/future).
        /// </summary>
        [HttpGet("{id}/time-slots")]
        public async Task<ActionResult<IEnumerable<ExpertTimeSlotResponse>>> GetExpertTimeSlots(Guid id)
        {
            var result = await _expertService.GetAvailableTimeSlotsAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// Get reviews/feedback for an expert.
        /// </summary>
        [HttpGet("{id}/reviews")]
        public async Task<ActionResult<PagingResponse<UserFeedbackResponse>>> GetExpertReviews(Guid id, [FromQuery] PaginationRequest request)
        {
            var result = await _expertService.GetExpertReviewsAsync(id, request);
            return Ok(result);
        }
    }
}
