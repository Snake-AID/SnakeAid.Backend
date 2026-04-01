using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Requests.AppNotification
{
    public class UpdateAppNotificationRequest
    {
        [Required(ErrorMessage = "IsRead is required")]
        public bool? IsRead { get; set; }
    }
}