using SnakeAid.Service.Implements.Email.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAid.Service.Implements.Email.Cores
{
    /// <summary>
    /// Interface for email sending operations
    /// </summary>
    public interface IEmailSender
    {
        /// <summary>
        /// Send an email using the configured provider
        /// </summary>
        Task<EmailSendResult> SendAsync(EmailSendRequest request, CancellationToken cancellationToken = default);
    }
}
