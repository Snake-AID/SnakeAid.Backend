using SnakeAid.Core.Domains;
using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Requests.User
{
    public class AdminCreateRescuerRequest
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Full name is required")]
        [MaxLength(200, ErrorMessage = "Full name must be at most 200 characters")]
        public string FullName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Invalid phone number format")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Rescuer type is required")]
        public RescuerType Type { get; set; }
    }
}
