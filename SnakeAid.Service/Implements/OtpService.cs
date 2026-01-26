using Microsoft.Extensions.Caching.Memory;
using SnakeAid.Core.Responses.Otp;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Service.Implements
{
    public class OtpService : IOtpService
    {
        private readonly IMemoryCache _cache;
        private const int OTP_EXPIRY_MINUTES = 5;
        private const int MAX_ATTEMPTS = 3;

        public OtpService(IMemoryCache cache)
        {
            _cache = cache;
        }

        private class OtpData
        {
            public string OtpCode { get; set; } = string.Empty;
            public DateTime ExpirationTime { get; set; }
            public int AttemptsLeft { get; set; }
        }

        public Task CreateOtpEntity(string email, string otp)
        {
            var cacheKey = $"otp_{email.ToLowerInvariant()}";
            
            var otpData = new OtpData
            {
                OtpCode = otp,
                ExpirationTime = DateTime.UtcNow.AddMinutes(OTP_EXPIRY_MINUTES),
                AttemptsLeft = MAX_ATTEMPTS
            };

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(OTP_EXPIRY_MINUTES));

            _cache.Set(cacheKey, otpData, cacheOptions);
            
            return Task.CompletedTask;
        }

        public Task<ValidateOtpResponse> CheckOtp(string email, string otp)
        {
            var cacheKey = $"otp_{email.ToLowerInvariant()}";
            
            if (!_cache.TryGetValue<OtpData>(cacheKey, out var otpData) || otpData == null)
            {
                return Task.FromResult(new ValidateOtpResponse
                {
                    Success = false,
                    AttemptsLeft = 0,
                    Message = "No OTP found for this email. Please request a new OTP."
                });
            }

            if (otpData.ExpirationTime < DateTime.UtcNow)
            {
                _cache.Remove(cacheKey);
                return Task.FromResult(new ValidateOtpResponse
                {
                    Success = false,
                    AttemptsLeft = 0,
                    Message = "OTP has expired. Please request a new OTP."
                });
            }

            if (otpData.OtpCode != otp)
            {
                otpData.AttemptsLeft--;

                if (otpData.AttemptsLeft <= 0)
                {
                    _cache.Remove(cacheKey);
                    return Task.FromResult(new ValidateOtpResponse
                    {
                        Success = false,
                        AttemptsLeft = 0,
                        Message = "Invalid OTP. Maximum attempts exceeded. Please request a new OTP."
                    });
                }
                else
                {
                    // Update attempts left in cache
                    var cacheOptions = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(otpData.ExpirationTime);
                    _cache.Set(cacheKey, otpData, cacheOptions);
                    
                    return Task.FromResult(new ValidateOtpResponse
                    {
                        Success = false,
                        AttemptsLeft = otpData.AttemptsLeft,
                        Message = $"Invalid OTP. You have {otpData.AttemptsLeft} attempt(s) left."
                    });
                }
            }

            return Task.FromResult(new ValidateOtpResponse
            {
                Success = true,
                AttemptsLeft = otpData.AttemptsLeft,
                Message = "OTP validated successfully."
            });
        }

        public Task<ValidateOtpResponse> ValidateOtp(string email, string otp)
        {
            var cacheKey = $"otp_{email.ToLowerInvariant()}";
            
            if (!_cache.TryGetValue<OtpData>(cacheKey, out var otpData) || otpData == null)
            {
                return Task.FromResult(new ValidateOtpResponse
                {
                    Success = false,
                    AttemptsLeft = 0,
                    Message = "No OTP found for this email. Please request a new OTP."
                });
            }

            if (otpData.ExpirationTime < DateTime.UtcNow)
            {
                _cache.Remove(cacheKey);
                return Task.FromResult(new ValidateOtpResponse
                {
                    Success = false,
                    AttemptsLeft = 0,
                    Message = "OTP has expired. Please request a new OTP."
                });
            }

            if (otpData.OtpCode != otp)
            {
                otpData.AttemptsLeft--;

                if (otpData.AttemptsLeft <= 0)
                {
                    _cache.Remove(cacheKey);
                    return Task.FromResult(new ValidateOtpResponse
                    {
                        Success = false,
                        AttemptsLeft = 0,
                        Message = "Invalid OTP. Maximum attempts exceeded. Please request a new OTP."
                    });
                }
                else
                {
                    // Update attempts left in cache
                    var cacheOptions = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(otpData.ExpirationTime);
                    _cache.Set(cacheKey, otpData, cacheOptions);
                    
                    return Task.FromResult(new ValidateOtpResponse
                    {
                        Success = false,
                        AttemptsLeft = otpData.AttemptsLeft,
                        Message = $"Invalid OTP. You have {otpData.AttemptsLeft} attempt(s) left."
                    });
                }
            }

            // OTP is valid - remove from cache (consume it)
            _cache.Remove(cacheKey);

            return Task.FromResult(new ValidateOtpResponse
            {
                Success = true,
                AttemptsLeft = otpData.AttemptsLeft,
                Message = "OTP validated successfully."
            });
        }
    }
}
