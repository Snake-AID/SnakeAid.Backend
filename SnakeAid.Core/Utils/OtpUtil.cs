using Microsoft.Extensions.Configuration;
using OtpNet;
using SnakeAid.Core.Domains;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAid.Core.Utils
{
    public class OtpUtil
    {
        private readonly string _secretSalt;

        public OtpUtil(IConfiguration configuration)
        {
            _secretSalt = configuration["OtpSettings:SecretSalt"];
        }

        private string GenerateSecretKey(string email)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secretSalt)))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(email));
                return Base32Encoding.ToString(hash);
            }
        }

        public virtual string GenerateOtp(string email)
        {
            var secretKey = GenerateSecretKey(email);
            var totp = new Totp(Base32Encoding.ToBytes(secretKey));
            var otp = totp.ComputeTotp();
            return otp;
        }
    }
}
