using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Enums;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.Auth;
using SnakeAid.Core.Responses.Auth;
using SnakeAid.Core.Settings;
using SnakeAid.Core.Utils;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using RescuerType = SnakeAid.Core.Domains.RescuerType;

namespace SnakeAid.Service.Implements;

public class AuthService : IAuthService
{
    private const string RefreshTokenProvider = "SnakeAid";
    private const string RefreshTokenName = "RefreshToken";
    private const string RefreshTokenExpiryName = "RefreshTokenExpiry";

    private readonly UserManager<Account> _userManager;
    private readonly SignInManager<Account> _signInManager;
    private readonly JwtSettings _jwtSettings;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;
    private readonly IOtpService _otpService;
    private readonly OtpUtil _otpUtil;
    private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;



    public AuthService(
        UserManager<Account> userManager,
        SignInManager<Account> signInManager,
        IOptions<JwtSettings> jwtSettings,
        IConfiguration configuration,
        ILogger<AuthService> logger,
        IOtpService otpService,
        IEmailService emailService,
        OtpUtil otpUtil,
        IUnitOfWork<SnakeAidDbContext> unitOfWork)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtSettings = jwtSettings.Value;
        _configuration = configuration;
        _logger = logger;
        _otpService = otpService;
        _unitOfWork = unitOfWork;
    }

    #region Public Methods

    public async Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request, RegisterRole? targetRole)
    {
        // Check if email already exists
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return ApiResponseBuilder.CreateResponse<AuthResponse>(
                null, false, "Email is already in use.", HttpStatusCode.BadRequest, "EMAIL_IN_USE");
        }

        // Create new account
        var user = new Account
        {
            Id = Guid.NewGuid(),
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName ?? string.Empty,
            PhoneNumber = request.PhoneNumber,
            IsActive = false,
            Role = AccountRole.User,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = new Dictionary<string, string[]>
            {
                ["Identity"] = result.Errors.Select(e => e.Description).ToArray()
            };
            return ApiResponseBuilder.CreateResponse<AuthResponse>(
                null, false, "Registration failed.", HttpStatusCode.UnprocessableEntity, "VALIDATION_ERROR", errors);
        }

        if (targetRole == null)
        {
            targetRole = RegisterRole.Member;
        }

        switch (targetRole)
        {
            case RegisterRole.Member:
                {
                    var memberRepository = _unitOfWork.GetRepository<MemberProfile>();
                    var member = new MemberProfile
                    {
                        AccountId = user.Id,
                        Rating = 0,
                        RatingCount = 0,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await memberRepository.InsertAsync(member);
                    await _unitOfWork.CommitAsync();
                    break;
                }

                case RegisterRole.Rescuer:
                {
                    var rescuerRepository = _unitOfWork.GetRepository<RescuerProfile>();
                    if (request.Type == null)
                    {
                        return ApiResponseBuilder.CreateResponse<AuthResponse>(
                            null, false, "Rescuer registration details are required.", HttpStatusCode.BadRequest, "MISSING_RESCUER_DETAILS");
                    }

                    var selectedType = RescuerType.Emergency;

                    switch (request.Type)
                    {
                        case RescuerType.Emergency:
                            selectedType = RescuerType.Emergency;
                            break;
                        case RescuerType.Catching:
                            selectedType = RescuerType.Catching;
                            break;
                        case RescuerType:
                            selectedType = RescuerType.Both;
                            break;
                    }

                    var rescuer = new RescuerProfile
                    {
                        AccountId = user.Id,
                        Type = selectedType,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await rescuerRepository.InsertAsync(rescuer);
                    await _unitOfWork.CommitAsync();
                    break;
                }

                case RegisterRole.Expert:
                {
                    var expertRepository = _unitOfWork.GetRepository<ExpertProfile>();
                    if (String.IsNullOrEmpty(request.Biography))
                    {
                        return ApiResponseBuilder.CreateResponse<AuthResponse>(
                            null, false, "Expert registration details are required.", HttpStatusCode.BadRequest, "MISSING_EXPERT_DETAILS");
                    }
                    var expert = new ExpertProfile
                    {
                        AccountId = user.Id,
                        Biography = request.Biography,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await expertRepository.InsertAsync(expert);
                    await _unitOfWork.CommitAsync();
                    break;
                }

        }

        _logger.LogInformation("User registered successfully: {Email}", request.Email);

        // Generate tokens
        var tokens = await GenerateTokensAsync(user);
        return ApiResponseBuilder.BuildSuccessResponse(tokens, "Registration successful.");
    }

    public async Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request)
    {
        // Find user by email
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return ApiResponseBuilder.CreateResponse<AuthResponse>(
                null, false, "Invalid email or password.", HttpStatusCode.Unauthorized, "INVALID_CREDENTIALS");
        }

        // Check if account is active
        if (!user.IsActive)
        {
            return ApiResponseBuilder.CreateResponse<AuthResponse>(
                null, false, "Account is inactive.", HttpStatusCode.Forbidden, "ACCOUNT_INACTIVE");
        }

        // Check password with lockout
        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            _logger.LogWarning("Account locked out: {Email}", request.Email);
            return ApiResponseBuilder.CreateResponse<AuthResponse>(
                null, false, "Account is locked. Please try again later.", HttpStatusCode.Forbidden, "ACCOUNT_LOCKED");
        }

        if (!result.Succeeded)
        {
            return ApiResponseBuilder.CreateResponse<AuthResponse>(
                null, false, "Invalid email or password.", HttpStatusCode.Unauthorized, "INVALID_CREDENTIALS");
        }

        _logger.LogInformation("User logged in: {Email}", request.Email);

        // Generate tokens
        var tokens = await GenerateTokensAsync(user);
        return ApiResponseBuilder.BuildSuccessResponse(tokens, "Login successful.");
    }

    public async Task<ApiResponse<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request)
    {
        // Find user
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user == null)
        {
            return ApiResponseBuilder.CreateResponse<AuthResponse>(
                null, false, "Invalid refresh token.", HttpStatusCode.Unauthorized, "INVALID_TOKEN");
        }

        // Check if account is active
        if (!user.IsActive)
        {
            return ApiResponseBuilder.CreateResponse<AuthResponse>(
                null, false, "Account is inactive.", HttpStatusCode.Forbidden, "ACCOUNT_INACTIVE");
        }

        // Validate refresh token
        var isValid = await ValidateRefreshTokenAsync(user, request.RefreshToken);
        if (!isValid)
        {
            return ApiResponseBuilder.CreateResponse<AuthResponse>(
                null, false, "Invalid or expired refresh token.", HttpStatusCode.Unauthorized, "INVALID_TOKEN");
        }

        // Token rotation: Remove old tokens
        await _userManager.RemoveAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenName);
        await _userManager.RemoveAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenExpiryName);

        _logger.LogInformation("Token refreshed for user: {Email}", user.Email);

        // Generate new tokens
        var tokens = await GenerateTokensAsync(user);
        return ApiResponseBuilder.BuildSuccessResponse(tokens, "Token refreshed successfully.");
    }

    public async Task<ApiResponse<AuthResponse>> GoogleLoginAsync(GoogleLoginRequest request)
    {
        // Get Google Client ID
        var clientId = _configuration["Authentication:Google:ClientId"];
        if (string.IsNullOrWhiteSpace(clientId))
        {
            _logger.LogError("Google Client ID is not configured");
            return ApiResponseBuilder.CreateResponse<AuthResponse>(
                null, false, "Google authentication is not configured.", HttpStatusCode.BadRequest, "GOOGLE_CONFIG_ERROR");
        }

        // Validate Google ID token
        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { clientId }
                });
        }
        catch (InvalidJwtException ex)
        {
            _logger.LogWarning("Invalid Google token: {Message}", ex.Message);
            return ApiResponseBuilder.CreateResponse<AuthResponse>(
                null, false, "Invalid Google token.", HttpStatusCode.Unauthorized, "INVALID_GOOGLE_TOKEN");
        }

        // Check if email is verified
        if (!payload.EmailVerified)
        {
            return ApiResponseBuilder.CreateResponse<AuthResponse>(
                null, false, "Google email is not verified.", HttpStatusCode.Unauthorized, "EMAIL_NOT_VERIFIED");
        }

        // Find or create user
        var user = await _userManager.FindByEmailAsync(payload.Email);
        if (user == null)
        {
            // Create new user
            user = new Account
            {
                Id = Guid.NewGuid(),
                UserName = payload.Email,
                Email = payload.Email,
                EmailConfirmed = true,
                FullName = payload.Name ?? string.Empty,
                AvatarUrl = payload.Picture,
                IsActive = true,
                Role = AccountRole.User,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                var errors = new Dictionary<string, string[]>
                {
                    ["Identity"] = createResult.Errors.Select(e => e.Description).ToArray()
                };
                return ApiResponseBuilder.CreateResponse<AuthResponse>(
                    null, false, "Failed to create account.", HttpStatusCode.UnprocessableEntity, "ACCOUNT_CREATE_FAILED", errors);
            }

            _logger.LogInformation("New user created via Google: {Email}", payload.Email);
        }

        // Check if account is active
        if (!user.IsActive)
        {
            return ApiResponseBuilder.CreateResponse<AuthResponse>(
                null, false, "Account is inactive.", HttpStatusCode.Forbidden, "ACCOUNT_INACTIVE");
        }

        // Link Google login if not already linked
        var logins = await _userManager.GetLoginsAsync(user);
        if (logins.All(l => l.LoginProvider != "Google"))
        {
            var loginInfo = new UserLoginInfo("Google", payload.Subject, "Google");
            await _userManager.AddLoginAsync(user, loginInfo);
        }

        _logger.LogInformation("Google login successful: {Email}", payload.Email);

        // Generate tokens
        var tokens = await GenerateTokensAsync(user);
        return ApiResponseBuilder.BuildSuccessResponse(tokens, "Google login successful.");
    }

    public async Task<ApiResponse<object>> LogoutAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return ApiResponseBuilder.BuildNotFoundResponse("User not found.");
        }

        // Remove refresh tokens
        await _userManager.RemoveAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenName);
        await _userManager.RemoveAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenExpiryName);

        _logger.LogInformation("User logged out: {Email}", user.Email);

        return ApiResponseBuilder.BuildSuccessResponse("Logged out successfully.");
    }

    public async Task<ApiResponse<VerifyAccountResponse>> VerifyAccountAsync(VerifyAccountRequest request)
    {
        // Find user by email
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return ApiResponseBuilder.CreateResponse<VerifyAccountResponse>(
                null, false, "User not found.", HttpStatusCode.NotFound, "USER_NOT_FOUND");
        }

        // Check if already active
        if (user.IsActive)
        {
            var response = new VerifyAccountResponse
            {
                Success = true,
                Message = "Account is already verified and active."
            };
            return ApiResponseBuilder.BuildSuccessResponse(response, response.Message);
        }

        // Validate OTP
        var otpValidation = await _otpService.ValidateOtp(request.Email, request.Otp);
        if (!otpValidation.Success)
        {
            return ApiResponseBuilder.CreateResponse(
                new VerifyAccountResponse
                {
                    Success = false,
                    Message = otpValidation.Message,
                    AuthData = null
                },
                false,
                otpValidation.Message,
                HttpStatusCode.BadRequest,
                "OTP_VALIDATION_FAILED");
        }

        // Activate user account
        user.IsActive = true;
        user.UpdatedAt = DateTime.UtcNow;
        var updateResult = await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            _logger.LogError("Failed to activate user {Email}", request.Email);
            return ApiResponseBuilder.CreateResponse<VerifyAccountResponse>(
                null, false, "Failed to activate account.", HttpStatusCode.InternalServerError, "ACTIVATION_FAILED");
        }

        _logger.LogInformation("User account verified and activated successfully: {Email}", request.Email);

        // Generate tokens for immediate login
        var authTokens = await GenerateTokensAsync(user);
        var verifyResponse = new VerifyAccountResponse
        {
            Success = true,
            Message = "Account verified and activated successfully.",
            AuthData = authTokens
        };

        return ApiResponseBuilder.BuildSuccessResponse(verifyResponse, verifyResponse.Message);
    }

    #endregion

    #region Private Methods

    private async Task<AuthResponse> GenerateTokensAsync(Account user)
    {
        var accessExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);
        var refreshExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);

        var accessToken = GenerateAccessToken(user, accessExpiresAt);
        var refreshToken = GenerateRefreshToken();

        // Store refresh token
        await _userManager.SetAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenName, refreshToken);
        await _userManager.SetAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenExpiryName,
            refreshExpiresAt.ToString("O", CultureInfo.InvariantCulture));

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiresAt = accessExpiresAt,
            RefreshTokenExpiresAt = refreshExpiresAt,
            User = new UserInfo
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                AvatarUrl = user.AvatarUrl,
                Role = user.Role.ToString(),
                IsActive = user.IsActive
            }
        };
    }

    private string GenerateAccessToken(Account user, DateTime expiresAt)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("full_name", user.FullName ?? string.Empty),
            new("avatar_url", user.AvatarUrl ?? string.Empty),
            new("is_active", user.IsActive.ToString().ToLowerInvariant())
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    private async Task<bool> ValidateRefreshTokenAsync(Account user, string refreshToken)
    {
        var storedToken = await _userManager.GetAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenName);
        if (string.IsNullOrEmpty(storedToken) || !string.Equals(storedToken, refreshToken, StringComparison.Ordinal))
        {
            return false;
        }

        var expiryString = await _userManager.GetAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenExpiryName);
        if (string.IsNullOrWhiteSpace(expiryString))
        {
            return false;
        }

        if (!DateTime.TryParse(expiryString, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var expiry))
        {
            return false;
        }

        return expiry >= DateTime.UtcNow;
    }

    #endregion
}
