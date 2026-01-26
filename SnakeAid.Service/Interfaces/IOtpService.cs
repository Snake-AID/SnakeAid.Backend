using SnakeAid.Core.Responses.Otp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAid.Service.Interfaces
{
    public interface IOtpService
    {
        Task CreateOtpEntity(string email, string otp);
        Task<ValidateOtpResponse> CheckOtp(string email, string otp);
        Task<ValidateOtpResponse> ValidateOtp(string email, string otp);
    }
}
