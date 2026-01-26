using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.Auth;
using SnakeAid.Core.Responses.Auth;

namespace SnakeAid.Service.Interfaces;

public interface IAuthService
{
    /// <summary>
    /// Register a new user account
    /// </summary>
    Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request);

    /// <summary>
    /// Login with email and password
    /// </summary>
    Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request);

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    Task<ApiResponse<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request);

    /// <summary>
    /// Login or register with Google ID token
    /// </summary>
    Task<ApiResponse<AuthResponse>> GoogleLoginAsync(GoogleLoginRequest request);

    /// <summary>
    /// Logout - invalidate refresh token
    /// </summary>
    Task<ApiResponse<object>> LogoutAsync(Guid userId);

    /// <summary>
    /// Verify account with OTP and activate user
    /// </summary>
    Task<ApiResponse<VerifyAccountResponse>> VerifyAccountAsync(VerifyAccountRequest request);
}
