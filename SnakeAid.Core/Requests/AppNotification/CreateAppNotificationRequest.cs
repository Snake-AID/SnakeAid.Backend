using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Requests.AppNotification
{
    public class CreateAppNotificationRequest
    {
        [Required(ErrorMessage = "Title is required")]
        [MaxLength(255, ErrorMessage = "Title must not exceed 255 characters")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Message is required")]
        [MaxLength(2000, ErrorMessage = "Message must not exceed 2000 characters")]
        public string Message { get; set; } = string.Empty;
    }
}