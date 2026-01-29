using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RazorLight;
using SnakeAid.Service.Implements.Email.Models;
using SnakeAid.Service.Implements.Email.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAid.Service.Implements.Email
{
    /// <summary>
    /// Email template service with RazorLight templates, PreMailer CSS inlining, and provider strategy
    /// </summary>
    public class EmailTemplateService
    {
        private readonly ILogger<EmailTemplateService> _logger;
        private readonly IConfiguration _configuration;
        private readonly RazorLightEngine _razorEngine;
        private readonly EmailProviderService _emailProvider;
        private readonly string _templateRoot;

        public EmailTemplateService(
            ILogger<EmailTemplateService> logger,
            IConfiguration configuration,
            EmailProviderService emailProvider)
        {
            _logger = logger;
            _configuration = configuration;
            _emailProvider = emailProvider;

            // Initialize RazorLight engine
            // Find template root in source code location, not bin output
            var baseDir = AppContext.BaseDirectory;
            var projectRoot = Directory.GetParent(baseDir)?.Parent?.Parent?.Parent?.Parent?.FullName;
            _templateRoot = Path.Combine(projectRoot!, "SnakeAid.Service", "Implements", "Email", "Templates");
            
            // Create directory if it doesn't exist
            if (!Directory.Exists(_templateRoot))
            {
                _logger.LogWarning("Template directory not found at: {TemplateRoot}", _templateRoot);
                Directory.CreateDirectory(_templateRoot);
            }
            
            _razorEngine = new RazorLightEngineBuilder()
                .UseFileSystemProject(_templateRoot)
                .UseMemoryCachingProvider()
                .Build();

            _logger.LogInformation("EmailTemplateService initialized with template root: {TemplateRoot}",
                _templateRoot);
        }

        /// <summary>
        /// Send OTP email using inline HTML template
        /// </summary>
        public async Task<EmailSendResult> SendOtpEmailAsync(OtpEmailModel model)
        {
            var result = new EmailSendResult { Success = false };

            try
            {
                // Step 1: Validate input
                if (string.IsNullOrWhiteSpace(model.RecipientEmail))
                {
                    throw new ArgumentException("Recipient email cannot be empty", nameof(model.RecipientEmail));
                }

                if (string.IsNullOrWhiteSpace(model.OtpCode))
                {
                    throw new ArgumentException("OTP code cannot be empty", nameof(model.OtpCode));
                }

                _logger.LogInformation("Starting OTP email send process for {Email}", model.RecipientEmail);

                // Step 2: Create HTML email with inline CSS
                var htmlBody = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>{model.Subject}</title>
</head>
<body style='font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; background-color: #f4f4f4; margin: 0; padding: 0;'>
    <div style='max-width: 600px; margin: 30px auto; background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 8px rgba(0,0,0,0.1);'>
        <div style='background: linear-gradient(135deg, #10b981 0%, #059669 100%); padding: 30px; text-align: center; color: white;'>
            <h1 style='margin: 0; font-size: 24px;'>🐍 SnakeAid Verification</h1>
        </div>
        <div style='padding: 40px 30px;'>
            <p style='font-size: 18px; margin-bottom: 20px; color: #2d3748;'>Hello {model.RecipientName ?? "there"},</p>
            <p style='font-size: 16px; margin-bottom: 30px; color: #4a5568;'>
                You have requested a One-Time Password (OTP) to verify your identity. 
                Please use the code below to complete your verification:
            </p>
            
            <div style='background-color: #f0fdf4; border: 2px dashed #10b981; border-radius: 8px; padding: 25px; text-align: center; margin: 30px 0;'>
                <div style='font-size: 14px; color: #718096; margin-bottom: 10px; text-transform: uppercase; letter-spacing: 1px;'>Your Verification Code</div>
                <div style='font-size: 36px; font-weight: bold; color: #059669; letter-spacing: 8px; font-family: ""Courier New"", monospace;'>{model.OtpCode}</div>
                <div style='margin-top: 15px; font-size: 14px; color: #dc2626; font-weight: 600;'>
                    ⏰ This code expires in {model.ExpiryMinutes} minutes
                </div>
            </div>

            <div style='background-color: #fef2f2; border-left: 4px solid #ef4444; padding: 15px; margin: 20px 0; font-size: 14px; color: #991b1b;'>
                <strong>⚠️ Security Notice:</strong> Never share this code with anyone. 
                <span style='font-weight: bold; color: #059669;'>SnakeAid</span> will never ask you to share your OTP via email, phone, or any other channel.
            </div>

            <p style='font-size: 16px; margin-bottom: 30px; color: #4a5568;'>
                If you didn't request this code, please ignore this email or contact our support team immediately.
            </p>
        </div>
        <div style='background-color: #f7fafc; padding: 20px 30px; text-align: center; font-size: 12px; color: #718096; border-top: 1px solid #e2e8f0;'>
            <p>This is an automated message from <span style='font-weight: bold; color: #059669;'>SnakeAid</span>. Please do not reply to this email.</p>
            <p>© {DateTime.UtcNow.Year} SnakeAid. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

                result.InlinedHtml = htmlBody;
                _logger.LogDebug("HTML template created successfully. Length: {Length} characters", htmlBody.Length);

                // Step 3: Send email via provider
                try
                {
                    var sendRequest = new EmailSendRequest
                    {
                        To = new[] { model.RecipientEmail },
                        Subject = model.Subject,
                        HtmlBody = htmlBody,
                        PlainTextBody = $"Your OTP code is: {model.OtpCode}\n\nThis code will expire in {model.ExpiryMinutes} minutes."
                    };

                    var sendResult = await _emailProvider.SendAsync(sendRequest);

                    result.Success = sendResult.Success;
                    result.MessageId = sendResult.MessageId;
                    result.Provider = sendResult.Provider;
                    result.ErrorMessage = sendResult.ErrorMessage;

                    if (sendResult.Success)
                    {
                        _logger.LogInformation(
                            "OTP email sent successfully to {Email} via {Provider}. Message ID: {MessageId}",
                            model.RecipientEmail,
                            sendResult.Provider,
                            sendResult.MessageId);
                    }
                    else
                    {
                        _logger.LogError("Failed to send OTP email: {Error}", sendResult.ErrorMessage);
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    var errorMsg = $"Failed to send email via provider: {ex.Message}";
                    _logger.LogError(ex, errorMsg);
                    result.ErrorMessage = errorMsg;
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in SendOtpEmailAsync");
                result.ErrorMessage = $"Unexpected error: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// Send a generic HTML email without using a template
        /// </summary>
        public async Task<EmailSendResult> SendHtmlEmailAsync(
            string recipientEmail,
            string subject,
            string htmlBody,
            string? plainTextBody = null)
        {
            try
            {
                _logger.LogInformation("Sending HTML email to {Email}", recipientEmail);

                var request = new EmailSendRequest
                {
                    To = new[] { recipientEmail },
                    Subject = subject,
                    HtmlBody = htmlBody,
                    PlainTextBody = plainTextBody ?? StripHtmlTags(htmlBody)
                };

                var result = await _emailProvider.SendAsync(request);

                if (result.Success)
                {
                    _logger.LogInformation(
                        "HTML email sent successfully to {Email} via {Provider}. Message ID: {MessageId}",
                        recipientEmail,
                        result.Provider,
                        result.MessageId);
                }
                else
                {
                    _logger.LogError("Failed to send HTML email to {Email}: {Error}",
                        recipientEmail, result.ErrorMessage);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending HTML email to {Email}", recipientEmail);
                return new EmailSendResult
                {
                    Success = false,
                    ErrorMessage = $"Error sending email: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Strip HTML tags from content to create plain text version
        /// </summary>
        private string StripHtmlTags(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return string.Empty;

            return System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", string.Empty);
        }
    }
}
