using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Settings;
using SnakeAid.Core.Validators;

namespace SnakeAid.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private const string RefreshTokenProvider = "SnakeAid";
        private const string RefreshTokenName = "RefreshToken";
        private const string RefreshTokenExpiryName = "RefreshTokenExpiry";

        private readonly UserManager<Account> _userManager;
        private readonly SignInManager<Account> _signInManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly JwtSettings _jwtSettings;
        private readonly IConfiguration _configuration;

        public AuthController(
            UserManager<Account> userManager,
            SignInManager<Account> signInManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            IOptions<JwtSettings> jwtOptions,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _jwtSettings = jwtOptions.Value;
            _configuration = configuration;
        }

        [HttpPost("register")]
        [ValidateModel]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return BadRequest(ApiResponseBuilder.BuildErrorResponse("Email is already in use.", "EMAIL_IN_USE"));
            }

            var user = new Account
            {
                Id = Guid.NewGuid(),
                UserName = request.Email,
                Email = request.Email,
                FullName = request.FullName ?? string.Empty,
                PhoneNumber = request.PhoneNumber,
                IsActive = true,
                PhoneVerified = false
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                return IdentityError(result, "Registration failed");
            }

            if (await _roleManager.RoleExistsAsync(AccountRole.User.ToString()))
            {
                await _userManager.AddToRoleAsync(user, AccountRole.User.ToString());
            }

            var tokens = await GenerateTokensAsync(user);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(tokens, "Registration successful"));
        }

        [HttpPost("login")]
        [ValidateModel]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return Unauthorized(ApiResponseBuilder.BuildUnauthorizedResponse("Invalid email or password."));
            }

            if (!user.IsActive)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    ApiResponseBuilder.BuildForbiddenResponse("Account is inactive."));
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            if (result.IsLockedOut)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    ApiResponseBuilder.BuildForbiddenResponse("Account is locked."));
            }

            if (!result.Succeeded)
            {
                return Unauthorized(ApiResponseBuilder.BuildUnauthorizedResponse("Invalid email or password."));
            }

            var tokens = await GenerateTokensAsync(user);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(tokens, "Login successful"));
        }

        [HttpPost("refresh")]
        [ValidateModel]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
        {
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user == null)
            {
                return Unauthorized(ApiResponseBuilder.BuildUnauthorizedResponse("Invalid refresh token."));
            }

            if (!user.IsActive)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    ApiResponseBuilder.BuildForbiddenResponse("Account is inactive."));
            }

            var isValid = await ValidateRefreshTokenAsync(user, request.RefreshToken);
            if (!isValid)
            {
                return Unauthorized(ApiResponseBuilder.BuildUnauthorizedResponse("Invalid refresh token."));
            }

            // Token Rotation: Remove old refresh token before issuing new one
            await _userManager.RemoveAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenName);
            await _userManager.RemoveAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenExpiryName);

            var tokens = await GenerateTokensAsync(user);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(tokens, "Token refreshed"));
        }

        [HttpPost("google")]
        [ValidateModel]
        public async Task<IActionResult> GoogleSignIn([FromBody] GoogleLoginRequest request)
        {
            var clientId = _configuration["Authentication:Google:ClientId"];
            if (string.IsNullOrWhiteSpace(clientId))
            {
                return BadRequest(ApiResponseBuilder.BuildErrorResponse("Google client ID is not configured.", "GOOGLE_CONFIG"));
            }

            GoogleJsonWebSignature.Payload payload;
            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken,
                    new GoogleJsonWebSignature.ValidationSettings
                    {
                        Audience = new[] { clientId }
                    });
            }
            catch
            {
                return Unauthorized(ApiResponseBuilder.BuildUnauthorizedResponse("Invalid Google token."));
            }

            if (!payload.EmailVerified)
            {
                return Unauthorized(ApiResponseBuilder.BuildUnauthorizedResponse("Google email is not verified."));
            }

            var user = await _userManager.FindByEmailAsync(payload.Email);
            if (user == null)
            {
                user = new Account
                {
                    Id = Guid.NewGuid(),
                    UserName = payload.Email,
                    Email = payload.Email,
                    EmailConfirmed = payload.EmailVerified,
                    FullName = payload.Name ?? string.Empty,
                    AvatarUrl = payload.Picture,
                    IsActive = true
                };

                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    return IdentityError(createResult, "Google sign-in failed");
                }

                if (await _roleManager.RoleExistsAsync(AccountRole.User.ToString()))
                {
                    await _userManager.AddToRoleAsync(user, AccountRole.User.ToString());
                }
            }

            if (!user.IsActive)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    ApiResponseBuilder.BuildForbiddenResponse("Account is inactive."));
            }

            var userLoginInfo = new UserLoginInfo("Google", payload.Subject, "Google");
            var logins = await _userManager.GetLoginsAsync(user);
            if (logins.All(l => l.LoginProvider != "Google"))
            {
                await _userManager.AddLoginAsync(user, userLoginInfo);
            }

            var tokens = await GenerateTokensAsync(user);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(tokens, "Google sign-in successful"));
        }

        private async Task<AuthResponse> GenerateTokensAsync(Account user)
        {
            var accessExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);
            var refreshExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);

            var accessToken = await GenerateAccessTokenAsync(user, accessExpiresAt);
            var refreshToken = GenerateRefreshToken();

            await _userManager.SetAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenName, refreshToken);
            await _userManager.SetAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenExpiryName,
                refreshExpiresAt.ToString("O"));

            return new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiresAt = accessExpiresAt,
                RefreshTokenExpiresAt = refreshExpiresAt
            };
        }

        private async Task<string> GenerateAccessTokenAsync(Account user, DateTime expiresAt)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new(JwtRegisteredClaimNames.UniqueName, user.UserName ?? string.Empty),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.UserName ?? string.Empty),
                new(ClaimTypes.Email, user.Email ?? string.Empty),
                new("full_name", user.FullName ?? string.Empty),
                new("phone_number", user.PhoneNumber ?? string.Empty),
                new("is_active", user.IsActive.ToString().ToLowerInvariant())
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                _jwtSettings.Issuer,
                _jwtSettings.Audience,
                claims,
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

            if (!DateTime.TryParse(expiryString, null, DateTimeStyles.RoundtripKind, out var expiry))
            {
                return false;
            }

            return expiry >= DateTime.UtcNow;
        }

        private static IActionResult IdentityError(IdentityResult result, string message)
        {
            var errors = new Dictionary<string, string[]>
            {
                ["Identity"] = result.Errors.Select(e => e.Description).ToArray()
            };

            var response = ApiResponseBuilder.BuildValidationErrorResponse<object>(errors, message);
            return new ObjectResult(response)
            {
                StatusCode = StatusCodes.Status422UnprocessableEntity
            };
        }
    }

    public class RegisterRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
    }

    public class LoginRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class RefreshRequest
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class GoogleLoginRequest
    {
        [Required]
        public string IdToken { get; set; } = string.Empty;
    }

    public class AuthResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime AccessTokenExpiresAt { get; set; }
        public DateTime RefreshTokenExpiresAt { get; set; }
    }
}
