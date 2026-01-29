using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Tsp;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Requests.Email;
using SnakeAid.Core.Responses.Email;
using SnakeAid.Core.Utils;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Implements.Email.Models;
using SnakeAid.Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAid.Service.Implements.Email
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly OtpUtil _otpUtil;
        private readonly IOtpService _otpService;
        private readonly EmailTemplateService _templateService;

        public EmailService(
            IUnitOfWork<SnakeAidDbContext> unitOfWork,
            ILogger<EmailService> logger,
            IOtpService otpService,
            OtpUtil otpUtil,
            EmailTemplateService templateService)
        {
            _logger = logger;
            _otpService = otpService;
            _unitOfWork = unitOfWork;
            _otpUtil = otpUtil;
            _templateService = templateService;
        }

        /// <summary>
        /// Send OTP email - delegates to EmailTemplateService
        /// </summary>
        public async Task<SendOtpEmailResponse> SendOtpEmailAsync(SendOtpEmailRequest request)
        {
            var response = new SendOtpEmailResponse();
            try
            {
                // Validate user exists
                var existingUser = await _unitOfWork.GetRepository<Account>().FirstOrDefaultAsync(predicate: s => s.Email == request.Email);
                if (existingUser == null)
                {
                    throw new NotFoundException($"User with email {request.Email} not found.");
                }

                // Generate OTP
                var otp = _otpUtil.GenerateOtp(request.Email);
                var expirationTime = DateTime.UtcNow.AddMinutes(5);

                // Create OTP entity in database
                await _otpService.CreateOtpEntity(request.Email, otp);

                // Prepare email model
                var recipientName = existingUser.FullName;

                var emailModel = new OtpEmailModel
                {
                    RecipientEmail = request.Email,
                    OtpCode = otp,
                    Expiration = expirationTime,
                    Subject = "Your SnakeAid Verification Code",
                    RecipientName = recipientName,
                    ExpiryMinutes = 5
                };

                // Send email using EmailTemplateService
                var emailResult = await _templateService.SendOtpEmailAsync(emailModel);

                if (!emailResult.Success)
                {
                    _logger.LogError("Failed to send OTP email: {Error}", emailResult.ErrorMessage);
                    throw new Exception($"Failed to send OTP email: {emailResult.ErrorMessage}");
                }

                _logger.LogInformation("OTP email sent successfully to {Email}. Message ID: {MessageId}",
                    request.Email, emailResult.MessageId);

                response.Success = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send OTP email to {Email}", request.Email);
                response.Success = false;
                throw;
            }

            return response;
        }
    }
}
