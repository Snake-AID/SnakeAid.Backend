using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using SnakeAid.Core.Requests.User;
using SnakeAid.Core.Responses.User;
using SnakeAid.Service.Interfaces;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Validators;
using Swashbuckle.AspNetCore.Annotations;

namespace SnakeAid.Api.Controllers
{
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(Roles = "Admin")]
    public class AdminUserController : BaseController<AdminUserController>
    {
        private readonly IAdminUserService _userService;

        public AdminUserController(
            ILogger<AdminUserController> logger,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            IAdminUserService userService)
            : base(logger, httpContextAccessor, mapper)
        {
            _userService = userService;
        }

        /// <summary>
        /// Get paginated list of all users for admin dashboard
        /// </summary>
        /// <param name="role">Filter by role (User, Admin, Expert, Rescuer, Operator)</param>
        /// <param name="isActive">Filter by active status (true/false)</param>
        /// <param name="searchTerm">Search by username, full name, or email</param>
        /// <param name="page">Page number (default 1)</param>
        /// <param name="pageSize">Items per page (default 50)</param>
        [HttpGet("list")]
        public async Task<IActionResult> GetAdminUserList(
            [FromQuery] string? role = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] string? searchTerm = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var result = await _userService.GetAdminUsersAsync(role, isActive, searchTerm, page, pageSize);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "User list retrieved."));
        }

        /// <summary>
        /// Get detailed information about a specific user
        /// </summary>
        /// <param name="userId">User ID</param>
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetAdminUserDetail([FromRoute] Guid userId)
        {
            var result = await _userService.GetAdminUserDetailAsync(userId);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "User detail retrieved."));
        }

        /// <summary>
        /// Create a rescuer account for the system
        /// </summary>
        [HttpPost("create-rescuer")]
        [ValidateModel]
        [SwaggerOperation(Summary = "Create rescuer account", Description = "Admin creates a rescuer account for the system.")]
        [SwaggerResponse(200, "Rescuer account created successfully", typeof(ApiResponse<AdminUserDetailResponse>))]
        [SwaggerResponse(400, "Validation error or email already in use")]
        public async Task<IActionResult> CreateRescuer([FromBody] AdminCreateRescuerRequest request)
        {
            var result = await _userService.CreateRescuerAsync(request);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Rescuer account created successfully."));
        }

        /// <summary>
        /// Ban/deactivate a user account
        /// </summary>
        /// <param name="userId">User ID to ban</param>
        /// <param name="request">Ban request with reason and optional duration</param>
        [HttpPost("{userId}/ban")]
        [ValidateModel]
        public async Task<IActionResult> BanUser(
            [FromRoute] Guid userId,
            [FromBody] BanUserRequest request)
        {
            var result = await _userService.BanUserAsync(userId, request.Reason);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "User has been banned."));
        }

        /// <summary>
        /// Unban/reactivate a user account
        /// </summary>
        /// <param name="userId">User ID to unban</param>
        [HttpPost("{userId}/unban")]
        public async Task<IActionResult> UnbanUser([FromRoute] Guid userId)
        {
            var result = await _userService.UnbanUserAsync(userId);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "User has been unbanned."));
        }
    }
}
