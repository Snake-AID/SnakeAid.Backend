using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Requests.User
{
    /// <summary>
    /// Request to ban/deactivate a user account
    /// </summary>
    public class BanUserRequest
    {
        [Required(ErrorMessage = "Reason is required")]
        [MaxLength(500, ErrorMessage = "Reason must not exceed 500 characters")]
        public string Reason { get; set; } = string.Empty;
    }
}
