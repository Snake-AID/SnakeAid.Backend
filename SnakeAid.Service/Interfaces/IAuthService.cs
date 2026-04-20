using SnakeAid.Core.Enums;
using SnakeAid.Core.Requests.Auth;
using SnakeAid.Core.Responses.Auth;

namespace SnakeAid.Service.Interfaces;

public interface IAuthService
{
    /// <summary>
    /// Register a new user account
    /// </summary>
    Task<AuthResponse> RegisterAsync(RegisterRequest request, RegisterRole? targetRole);

    /// <summary>
    /// Login with email and password
    /// </summary>
    Task<AuthResponse> LoginAsync(LoginRequest request);

    /// <summary>
    /// Login with email and password
    /// </summary>
    Task<AuthResponse> LoginV2Async(LoginRequestV2 request);

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request);

    /// <summary>
    /// Login or register with Google ID token
    /// </summary>
    Task<AuthResponse> GoogleLoginAsync(GoogleLoginRequest request);

    /// <summary>
    /// Logout - invalidate refresh token
    /// </summary>
    Task LogoutAsync(Guid userId);

    /// <summary>
    /// Verify account with OTP and activate user
    /// </summary>
    Task<VerifyAccountResponse> VerifyAccountAsync(VerifyAccountRequest request);

    /// <summary>
    /// Change current user's password
    /// </summary>
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
}
