using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAid.Service.Implements.Email.Models
{
    public class EmailSendResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? RenderedTemplate { get; set; }
        public string? InlinedHtml { get; set; }
        public string? MessageId { get; set; }
        public string? Provider { get; set; }
    }
}
