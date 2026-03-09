using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Exceptions;
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
    [Route("api/experts")]
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
            if (!Guid.TryParse(userIdStr, out var expertId)) throw new UnauthorizedException("User ID not found in token.");

            await _expertService.UpdateSettingsAsync(expertId, request);
            return Ok(ApiResponseBuilder.BuildSuccessResponse("Settings updated successfully"));
        }

        /// <summary>
        /// Expert generates time slots for the week based on configurable time blocks.
        /// </summary>
        [HttpPost("me/time-slots/bulk")]
        [Authorize(Roles = "Expert")]
        public async Task<IActionResult> CreateBulkTimeSlots([FromBody] BulkTimeSlotRequest request)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdStr, out var expertId)) throw new UnauthorizedException("User ID not found in token.");

            await _expertService.CreateBulkTimeSlotsAsync(expertId, request);
            return Ok(ApiResponseBuilder.BuildSuccessResponse("Time slots generated successfully"));
        }

        /// <summary>
        /// List all experts (for patients to browse).
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagingResponse<ExpertProfileResponse>>>> GetExperts([FromQuery] ExpertDirectoryQueryRequest request)
        {
            var result = await _expertService.GetExpertsAsync(request);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
        }

        /// <summary>
        /// Get detailed profile for a specific expert.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<ExpertProfileResponse>>> GetExpertProfile(Guid id)
        {
            var result = await _expertService.GetExpertProfileAsync(id);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
        }

        /// <summary>
        /// Get expert's available time slots (for the immediate week/future).
        /// </summary>
        [HttpGet("{id}/time-slots")]
        public async Task<ActionResult<ApiResponse<IEnumerable<ExpertTimeSlotResponse>>>> GetExpertTimeSlots(Guid id)
        {
            var result = await _expertService.GetAvailableTimeSlotsAsync(id);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
        }

        /// <summary>
        /// Get reviews/feedback for an expert.
        /// </summary>
        [HttpGet("{id}/reviews")]
        public async Task<ActionResult<ApiResponse<PagingResponse<UserFeedbackResponse>>>> GetExpertReviews(Guid id, [FromQuery] PaginationRequest request)
        {
            var result = await _expertService.GetExpertReviewsAsync(id, request);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
        }
    }
}
