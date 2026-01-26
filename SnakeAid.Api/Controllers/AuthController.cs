using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.Auth;
using SnakeAid.Core.Responses.Auth;
using SnakeAid.Service.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SnakeAid.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : BaseController<AuthController>
{
    private readonly IAuthService _authService;

    public AuthController(
        ILogger<AuthController> logger,
        IHttpContextAccessor httpContextAccessor,
        IMapper mapper,
        IAuthService authService)
        : base(logger, httpContextAccessor, mapper)
    {
        _authService = authService;
    }

    /// <summary>
    /// Register a new user account
    /// </summary>
    [HttpPost("register")]
    [SwaggerOperation(Summary = "Register new user", Description = "Create a new user account with email and password")]
    [SwaggerResponse(200, "Registration successful", typeof(ApiResponse<AuthResponse>))]
    [SwaggerResponse(400, "Email already in use or validation error")]
    [SwaggerResponse(422, "Validation error")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Login with email and password
    /// </summary>
    [HttpPost("login")]
    [SwaggerOperation(Summary = "Login", Description = "Authenticate user with email and password")]
    [SwaggerResponse(200, "Login successful", typeof(ApiResponse<AuthResponse>))]
    [SwaggerResponse(401, "Invalid credentials")]
    [SwaggerResponse(403, "Account locked or inactive")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Refresh access token
    /// </summary>
    [HttpPost("refresh")]
    [SwaggerOperation(Summary = "Refresh token", Description = "Exchange refresh token for new access and refresh tokens")]
    [SwaggerResponse(200, "Token refreshed", typeof(ApiResponse<AuthResponse>))]
    [SwaggerResponse(401, "Invalid or expired refresh token")]
    [SwaggerResponse(403, "Account inactive")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.RefreshTokenAsync(request);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Login or register with Google
    /// </summary>
    [HttpPost("google")]
    [SwaggerOperation(Summary = "Google Sign-In", Description = "Authenticate or register with Google ID token")]
    [SwaggerResponse(200, "Google login successful", typeof(ApiResponse<AuthResponse>))]
    [SwaggerResponse(400, "Google authentication not configured")]
    [SwaggerResponse(401, "Invalid Google token or email not verified")]
    [SwaggerResponse(403, "Account inactive")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
    {
        var result = await _authService.GoogleLoginAsync(request);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Logout current user
    /// </summary>
    [Authorize]
    [HttpPost("logout")]
    [SwaggerOperation(Summary = "Logout", Description = "Invalidate refresh token and logout")]
    [SwaggerResponse(200, "Logged out successfully")]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(404, "User not found")]
    public async Task<IActionResult> Logout()
    {
        var userId = GetCurrentUserId();
        var result = await _authService.LogoutAsync(userId);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Verify account with OTP
    /// </summary>
    [HttpPost("verify-account")]
    [SwaggerOperation(Summary = "Verify account", Description = "Verify user account with OTP code and activate it.")]
    [SwaggerResponse(200, "Account verified successfully", typeof(ApiResponse<VerifyAccountResponse>))]
    [SwaggerResponse(400, "Invalid OTP or validation error")]
    [SwaggerResponse(404, "User not found")]
    [SwaggerResponse(500, "Failed to activate account")]
    public async Task<IActionResult> VerifyAccount([FromBody] VerifyAccountRequest request)
    {
        var result = await _authService.VerifyAccountAsync(request);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get current user info
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    [SwaggerOperation(Summary = "Get current user", Description = "Get authenticated user information")]
    [SwaggerResponse(200, "User info retrieved", typeof(ApiResponse<UserInfo>))]
    [SwaggerResponse(401, "Unauthorized")]
    public IActionResult GetCurrentUser()
    {
        var user = HttpContext.User;
        var userInfo = new UserInfo
        {
            Id = GetCurrentUserId(),
            Email = GetCurrentUserEmail(),
            FullName = user.FindFirst("full_name")?.Value ?? string.Empty,
            AvatarUrl = user.FindFirst("avatar_url")?.Value,
            Role = user.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? string.Empty,
            IsActive = bool.TryParse(user.FindFirst("is_active")?.Value, out var isActive) && isActive
        };

        return Ok(ApiResponseBuilder.BuildSuccessResponse(userInfo, "User info retrieved successfully."));
    }
}
