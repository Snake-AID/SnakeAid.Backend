using SnakeAid.Core.Domains;
using SnakeAid.Core.Responses.Otp;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace SnakeAid.Service.Implements
{
    public class OtpService : IOtpService
    {
        private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;

        public OtpService(IUnitOfWork<SnakeAidDbContext> unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task CreateOtpEntity(string email, string otp)
        {
            var otpRepository = _unitOfWork.GetRepository<Otp>();

            var existingOtp = await otpRepository.FirstOrDefaultAsync(predicate: o => o.Email == email);
            if (existingOtp != null)
            {
                otpRepository.Delete(existingOtp);
                await _unitOfWork.CommitAsync();
            }

            var otpEntity = new Otp
            {
                Id = Guid.NewGuid(),
                Email = email,
                OtpCode = otp,
                ExpirationTime = DateTime.UtcNow.AddMinutes(5),
                AttemptLeft = 3
            };

            await otpRepository.InsertAsync(otpEntity);
            await _unitOfWork.CommitAsync();
        }

        public async Task<ValidateOtpResponse> CheckOtp(string email, string otp)
        {
            var otpRepository = _unitOfWork.GetRepository<Otp>();
            var otpEntity = await otpRepository.FirstOrDefaultAsync(predicate: o => o.Email == email);

            if (otpEntity == null)
            {
                return new ValidateOtpResponse
                {
                    Success = false,
                    AttemptsLeft = 0,
                    Message = "No OTP found for this email. Please request a new OTP."
                };
            }

            if (otpEntity.ExpirationTime < DateTime.UtcNow)
            {
                otpRepository.Delete(otpEntity);
                await _unitOfWork.CommitAsync();
                return new ValidateOtpResponse
                {
                    Success = false,
                    AttemptsLeft = 0,
                    Message = "OTP has expired. Please request a new OTP."
                };
            }

            if (otpEntity.OtpCode != otp)
            {
                otpEntity.AttemptLeft--;

                if (otpEntity.AttemptLeft <= 0)
                {
                    otpRepository.Delete(otpEntity);
                    await _unitOfWork.CommitAsync();
                    return new ValidateOtpResponse
                    {
                        Success = false,
                        AttemptsLeft = 0,
                        Message = "Invalid OTP. Maximum attempts exceeded. Please request a new OTP."
                    };
                }
                else
                {
                    otpRepository.Update(otpEntity);
                    await _unitOfWork.CommitAsync();
                    return new ValidateOtpResponse
                    {
                        Success = false,
                        AttemptsLeft = otpEntity.AttemptLeft,
                        Message = $"Invalid OTP. You have {otpEntity.AttemptLeft} attempt(s) left."
                    };
                }
            }

            return new ValidateOtpResponse
            {
                Success = true,
                AttemptsLeft = otpEntity.AttemptLeft,
                Message = "OTP validated successfully."
            };
        }

        public async Task<ValidateOtpResponse> ValidateOtp(string email, string otp)
        {
            var otpRepository = _unitOfWork.GetRepository<Otp>();
            var otpEntity = await otpRepository.FirstOrDefaultAsync(predicate: o => o.Email == email);

            if (otpEntity == null)
            {
                return new ValidateOtpResponse
                {
                    Success = false,
                    AttemptsLeft = 0,
                    Message = "No OTP found for this email. Please request a new OTP."
                };
            }

            if (otpEntity.ExpirationTime < DateTime.UtcNow)
            {
                otpRepository.Delete(otpEntity);
                await _unitOfWork.CommitAsync();
                return new ValidateOtpResponse
                {
                    Success = false,
                    AttemptsLeft = 0,
                    Message = "OTP has expired. Please request a new OTP."
                };
            }

            if (otpEntity.OtpCode != otp)
            {
                otpEntity.AttemptLeft--;

                if (otpEntity.AttemptLeft <= 0)
                {
                    otpRepository.Delete(otpEntity);
                    await _unitOfWork.CommitAsync();
                    return new ValidateOtpResponse
                    {
                        Success = false,
                        AttemptsLeft = 0,
                        Message = "Invalid OTP. Maximum attempts exceeded. Please request a new OTP."
                    };
                }
                else
                {
                    otpRepository.Update(otpEntity);
                    await _unitOfWork.CommitAsync();
                    return new ValidateOtpResponse
                    {
                        Success = false,
                        AttemptsLeft = otpEntity.AttemptLeft,
                        Message = $"Invalid OTP. You have {otpEntity.AttemptLeft} attempt(s) left."
                    };
                }
            }

            otpRepository.Delete(otpEntity);
            await _unitOfWork.CommitAsync();

            return new ValidateOtpResponse
            {
                Success = true,
                AttemptsLeft = otpEntity.AttemptLeft,
                Message = "OTP validated successfully."
            };
        }
    }
}
