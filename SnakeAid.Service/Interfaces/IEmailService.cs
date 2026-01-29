using SnakeAid.Core.Requests.Email;
using SnakeAid.Core.Responses.Email;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAid.Service.Interfaces
{
    public interface IEmailService
    {
        Task<SendOtpEmailResponse> SendOtpEmailAsync(SendOtpEmailRequest request);
    }
}
