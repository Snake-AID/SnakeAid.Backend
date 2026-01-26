using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SnakeAid.Service.Implements.Email.Cores;
using SnakeAid.Service.Implements.Email.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAid.Service.Implements.Email.Providers
{
    /// <summary>
    /// Email service with provider strategy and automatic fallback
    /// </summary>
    public class EmailProviderService : IEmailSender
    {
        private readonly ILogger<EmailProviderService> _logger;
        private readonly IConfiguration _configuration;
        private readonly ResendEmailSender _resendSender;
        private readonly SmtpEmailSender _smtpSender;
        private readonly string _preferredProvider;
        private readonly bool _enableFallback;

        public EmailProviderService(
            ILogger<EmailProviderService> logger,
            IConfiguration configuration,
            ResendEmailSender resendSender,
            SmtpEmailSender smtpSender)
        {
            _logger = logger;
            _configuration = configuration;
            _resendSender = resendSender;
            _smtpSender = smtpSender;

            _preferredProvider = _configuration["EmailSettings:Provider"]?.ToUpper() ?? "SMTP";
            _enableFallback = bool.Parse(_configuration["EmailSettings:EnableFallback"] ?? "true");

            _logger.LogInformation("EmailProviderService initialized. Preferred: {Provider}, Fallback: {Fallback}",
                _preferredProvider, _enableFallback);
        }

        public async Task<EmailSendResult> SendAsync(EmailSendRequest request, CancellationToken cancellationToken = default)
        {
            IEmailSender primarySender;
            IEmailSender? fallbackSender = null;

            // Determine primary and fallback senders
            if (_preferredProvider == "RESEND")
            {
                primarySender = _resendSender;
                fallbackSender = _enableFallback ? _smtpSender : null;
            }
            else // Default to SMTP
            {
                primarySender = _smtpSender;
                fallbackSender = _enableFallback ? _resendSender : null;
            }

            // Try primary sender
            _logger.LogInformation("Attempting to send email via primary provider: {Provider}",
                _preferredProvider);

            var result = await primarySender.SendAsync(request, cancellationToken);

            if (result.Success)
            {
                return result;
            }

            // Try fallback if enabled and primary failed
            if (fallbackSender != null)
            {
                _logger.LogWarning("Primary provider failed: {Error}. Attempting fallback provider",
                    result.ErrorMessage);

                var fallbackResult = await fallbackSender.SendAsync(request, cancellationToken);

                if (fallbackResult.Success)
                {
                    _logger.LogInformation("Email sent successfully via fallback provider: {Provider}",
                        fallbackResult.Provider);
                    fallbackResult.ErrorMessage = $"Primary provider failed, sent via fallback. Original error: {result.ErrorMessage}";
                }

                return fallbackResult;
            }

            _logger.LogError("Email sending failed with no fallback available");
            return result;
        }
    }


}
