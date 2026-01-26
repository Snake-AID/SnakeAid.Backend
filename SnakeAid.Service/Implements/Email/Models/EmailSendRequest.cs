using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAid.Service.Implements.Email.Models
{
    public class EmailSendRequest
    {
        public required string[] To { get; set; }
        public string[]? Cc { get; set; }
        public string[]? Bcc { get; set; }
        public required string Subject { get; set; }
        public required string HtmlBody { get; set; }
        public string? PlainTextBody { get; set; }
        public string? ReplyTo { get; set; }
        public Dictionary<string, string>? Headers { get; set; }
    }
}
