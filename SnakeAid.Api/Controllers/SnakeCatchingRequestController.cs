using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.SnakeCatchingRequest;
using SnakeAid.Core.Responses.SnakeCatchingRequest;
using SnakeAid.Service.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SnakeAid.Api.Controllers
{
    [Route("api/snakecatching/requests")]
    [ApiController]
    [Authorize]
    public class SnakeCatchingRequestController : BaseController<SnakeCatchingRequestController>
    {
        private readonly ISnakeCatchingRequestService _snakeCatchingRequestService;

        public SnakeCatchingRequestController(
            ILogger<SnakeCatchingRequestController> logger,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            ISnakeCatchingRequestService snakeCatchingRequestService)
            : base(logger, httpContextAccessor, mapper)
        {
            _snakeCatchingRequestService = snakeCatchingRequestService;
        }

        /// <summary>
        /// Get all snake catching requests
        /// </summary>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Get All Snake Catching Requests",
            Description = "Retrieve all snake catching requests with user information, media, and snake species details. Results are ordered by request date (newest first)")]
        [SwaggerResponse(200, "Requests retrieved successfully", typeof(ApiResponse<List<ListSnakeCatchingRequestResponse>>))]
        [SwaggerResponse(401, "User not authenticated")]
        [ProducesResponseType(typeof(ApiResponse<List<ListSnakeCatchingRequestResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllSnakeCatchingRequests()
        {
            var result = await _snakeCatchingRequestService.GetAllRequestAsync();
            
            return Ok(ApiResponseBuilder.BuildSuccessResponse(
                result,
                $"Retrieved {result.Count} snake catching request(s) successfully."));
        }

        /// <summary>
        /// Create a new snake catching request
        /// </summary>
        [HttpPost]
        [SwaggerOperation(
            Summary = "Create Snake Catching Request",
            Description = "Submit a request for professional snake catching service")]
        [SwaggerResponse(200, "Request created successfully", typeof(ApiResponse<CreateSnakeCatchingRequestResponse>))]
        [SwaggerResponse(400, "Invalid request data or user not found")]
        [SwaggerResponse(401, "User not authenticated")]
        [SwaggerResponse(422, "Validation error")]
        [ProducesResponseType(typeof(ApiResponse<CreateSnakeCatchingRequestResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> CreateSnakeCatchingRequest([FromBody] CreateSnakeCatchingRequestRequest request)
        {
            var userId = GetCurrentUserId();

            var result = await _snakeCatchingRequestService.CreateSnakeCatchingRequestAsync(userId, request);
            
            return Ok(ApiResponseBuilder.BuildSuccessResponse(
                result,
                "Snake catching request created successfully! Our team will review and assign a rescuer soon."));
        }

        /// <summary>
        /// Accept a snake catching request as a rescuer
        /// </summary>
        /// <param name="requestId">The ID of the snake catching request to accept</param>
        [HttpPost("accept/{requestId:guid}")]
        [SwaggerOperation(
            Summary = "Accept Snake Catching Request",
            Description = "Rescuer accepts a pending snake catching request and creates a new mission")]
        [SwaggerResponse(200, "Request accepted successfully", typeof(ApiResponse<CreateSnakeCatchingRequestResponse>))]
        [SwaggerResponse(400, "Invalid request (already assigned, not pending, or rescuer offline)")]
        [SwaggerResponse(401, "User not authenticated")]
        [SwaggerResponse(403, "User is not a rescuer")]
        [SwaggerResponse(404, "Request not found")]
        [ProducesResponseType(typeof(ApiResponse<CreateSnakeCatchingRequestResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AcceptSnakeCatchingRequest([FromRoute] Guid requestId, [FromForm] AcceptSnakeCatchingRequestRequest request)
        {
            var rescuerId = GetCurrentUserId();

            var result = await _snakeCatchingRequestService.AcceptSnakeCatchingRequestAsync(rescuerId, requestId, request);
            
            return Ok(ApiResponseBuilder.BuildSuccessResponse(
                result,
                "Snake catching request accepted successfully! Mission created."));
        }

        /// <summary>
        /// Get snake catching request details by ID
        /// </summary>
        /// <param name="requestId">The ID of the snake catching request</param>
        [HttpGet("{requestId:guid}")]
        [SwaggerOperation(
            Summary = "Get Snake Catching Request Details",
            Description = "Retrieve detailed information about a specific snake catching request including user, rescuer, media, and mission information")]
        [SwaggerResponse(200, "Request details retrieved successfully", typeof(ApiResponse<CreateSnakeCatchingRequestResponse>))]
        [SwaggerResponse(401, "User not authenticated")]
        [SwaggerResponse(404, "Request not found")]
        [ProducesResponseType(typeof(ApiResponse<CreateSnakeCatchingRequestResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetSnakeCatchingRequestDetail([FromRoute] Guid requestId)
        {
            var result = await _snakeCatchingRequestService.GetDetailAsync(requestId);
            
            return Ok(ApiResponseBuilder.BuildSuccessResponse(
                result,
                "Snake catching request details retrieved successfully."));
        }
    }
}
