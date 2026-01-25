using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Requests.Auth;

public class RegisterRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    public string Password { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? FullName { get; set; }

    [Phone(ErrorMessage = "Invalid phone number format")]
    public string? PhoneNumber { get; set; }
}

public class LoginRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; } = string.Empty;
}

public class RefreshTokenRequest
{
    [Required(ErrorMessage = "User ID is required")]
    public Guid UserId { get; set; }

    [Required(ErrorMessage = "Refresh token is required")]
    public string RefreshToken { get; set; } = string.Empty;
}

public class GoogleLoginRequest
{
    [Required(ErrorMessage = "Google ID token is required")]
    public string IdToken { get; set; } = string.Empty;
}
