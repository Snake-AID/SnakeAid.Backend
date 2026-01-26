using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAid.Service.Implements.Email.Models
{
    public class OtpEmailModel
    {
        public string RecipientEmail { get; set; } = string.Empty;
        public string OtpCode { get; set; } = string.Empty;
        public DateTime Expiration { get; set; }
        public string Subject { get; set; } = string.Empty;

        // Additional properties for template rendering
        public string RecipientName { get; set; } = "User";
        public int ExpiryMinutes { get; set; } = 5;
    }
}
