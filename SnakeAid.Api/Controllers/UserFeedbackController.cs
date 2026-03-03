using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.UserFeedback;
using SnakeAid.Core.Responses.UserFeedback;
using SnakeAid.Service.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SnakeAid.Api.Controllers
{
    [ApiController]
    [Route("api/feedback")]
    [Authorize]
    public class UserFeedbackController : BaseController<UserFeedbackController>
    {
        private readonly IUserFeedbackService _feedbackService;

        public UserFeedbackController(
            ILogger<UserFeedbackController> logger,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            IUserFeedbackService feedbackService)
            : base(logger, httpContextAccessor, mapper)
        {
            _feedbackService = feedbackService;
        }

        /// <summary>
        /// Tạo feedback/đánh giá mới
        /// </summary>
        /// <remarks>
        /// Cho phép user đánh giá người dùng khác sau khi hoàn thành dịch vụ.
        /// 
        /// **Feedback Types:**
        /// - Emergency (0): Đánh giá cho rescuer trong rescue mission
        /// - Catching (1): Đánh giá cho rescuer trong snake catching mission
        /// - Consultation (2): Đánh giá cho expert trong consultation
        /// 
        /// **Target User Roles:**
        /// - User (0): Member profile
        /// - Expert (2): Expert profile
        /// - Rescuer (3): Rescuer profile
        /// 
        /// Hệ thống sẽ tự động:
        /// - Tính toán lại average rating của target user
        /// - Cập nhật rating count
        /// - Cập nhật vào profile tương ứng dựa theo role
        /// </remarks>
        [HttpPost]
        [SwaggerOperation(
            Summary = "Tạo feedback mới",
            Description = "Tạo feedback/đánh giá cho user khác sau khi hoàn thành dịch vụ"
        )]
        [SwaggerResponse(200, "Feedback created successfully", typeof(ApiResponse<UserFeedbackResponse>))]
        [SwaggerResponse(400, "Bad request - Invalid data or duplicate feedback")]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(404, "Target user or reference not found")]
        public async Task<IActionResult> CreateFeedback([FromBody] CreateUserFeedbackRequest request)
        {
            var raterId = GetCurrentUserId();
            var result = await _feedbackService.CreateFeedbackAsync(request, raterId);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Feedback created successfully."));
        }

        /// <summary>
        /// Lấy tất cả feedback của một user
        /// </summary>
        /// <remarks>
        /// Lấy danh sách tất cả feedback/đánh giá mà một user đã nhận.
        /// 
        /// Sắp xếp theo thời gian tạo mới nhất.
        /// 
        /// Kết quả bao gồm:
        /// - Thông tin người đánh giá (Rater)
        /// - Rating và comment
        /// - Loại feedback (Emergency/Catching/Consultation)
        /// - Reference ID liên quan
        /// </remarks>
        /// <param name="targetUserId">ID của user cần xem feedback</param>
        [HttpGet("user/{targetUserId}")]
        [SwaggerOperation(
            Summary = "Lấy tất cả feedback của user",
            Description = "Lấy danh sách feedback mà một user đã nhận"
        )]
        [SwaggerResponse(200, "Success", typeof(ApiResponse<List<UserFeedbackResponse>>))]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(404, "Target user not found")]
        public async Task<IActionResult> GetFeedbacksByTargetUserId([FromRoute] Guid targetUserId)
        {
            var result = await _feedbackService.GetFeedbacksByTargetUserIdAsync(targetUserId);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, $"Retrieved {result.Count} feedback(s)."));
        }
    }
}
