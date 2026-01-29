using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Requests.Otp;

public class ValidateOtpRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "OTP code is required")]
    [StringLength(10, MinimumLength = 6, ErrorMessage = "OTP must be between 6 and 10 characters")]
    public string Otp { get; set; } = string.Empty;
}
