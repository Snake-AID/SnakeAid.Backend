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
    [Route("api/snake-catching-requests")]
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
        /// Create a new snake catching request
        /// </summary>
        /// <remarks>
        /// Create a request for snake catching service. The request will be assigned to available rescuers based on location and priority.
        /// 
        /// **Requirements:**
        /// - User must be authenticated
        /// - Valid address and coordinates required
        /// - EstimatedPrice must be greater than 0 if provided
        /// - MediaURLList can contain uploaded media URLs
        /// 
        /// **Validations:**
        /// - Longitude: -180 to 180
        /// - Latitude: -90 to 90
        /// - RequestDate cannot be more than 5 minutes in the past
        /// - PreferredTime cannot be in the past
        /// - Address max length: 1000 characters
        /// - AdditionalDetails max length: 2000 characters
        /// - Notes max length: 1000 characters
        /// 
        /// **Example Request:**
        /// ```json
        /// {
        ///   "address": "123 Nguyen Hue, District 1, Ho Chi Minh City",
        ///   "latitude": "10.762622",
        ///   "longitude": "106.660172",
        ///   "additionalDetails": "Snake found in the garden, approximately 1 meter long, brown color",
        ///   "requestDate": "2024-01-20T10:30:00Z",
        ///   "preferredTime": "2024-01-20T14:00:00Z",
        ///   "estimatedPrice": 500000,
        ///   "notes": "Please bring necessary equipment",
        ///   "mediaURLList": [
        ///     "https://cloudinary.com/image1.jpg",
        ///     "https://cloudinary.com/image2.jpg"
        ///   ]
        /// }
        /// ```
        /// </remarks>
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
    }
}
