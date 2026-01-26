using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RazorLight;
using SnakeAid.Service.Implements.Email.Models;
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
            _templateRoot = Path.Combine(AppContext.BaseDirectory, "Services", "Implements", "Email", "Templates");
            _razorEngine = new RazorLightEngineBuilder()
                .UseFileSystemProject(_templateRoot)
                .UseMemoryCachingProvider()
                .Build();

            _logger.LogInformation("EmailTemplateService initialized with template root: {TemplateRoot}",
                _templateRoot);
        }

        /// <summary>
        /// Send OTP email using RazorLight template, PreMailer CSS inlining, and provider strategy
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

                // Step 2: Render template with RazorLight
                string renderedTemplate;
                try
                {
                    renderedTemplate = await _razorEngine.CompileRenderAsync("OtpEmail.cshtml", model);
                    result.RenderedTemplate = renderedTemplate;
                    _logger.LogDebug("Template rendered successfully. Length: {Length} characters",
                        renderedTemplate.Length);
                }
                catch (Exception ex)
                {
                    var errorMsg = $"Failed to render email template: {ex.Message}";
                    _logger.LogError(ex, errorMsg);
                    result.ErrorMessage = errorMsg;
                    return result;
                }

                // Step 3: Inline CSS with PreMailer
                string inlinedHtml;
                try
                {
                    var preMailer = new PreMailer.Net.PreMailer(renderedTemplate);
                    var inlineResult = preMailer.MoveCssInline();

                    if (inlineResult.Warnings != null && inlineResult.Warnings.Count > 0)
                    {
                        _logger.LogWarning("PreMailer warnings: {Warnings}",
                            string.Join(", ", inlineResult.Warnings));
                    }

                    inlinedHtml = inlineResult.Html;
                    result.InlinedHtml = inlinedHtml;
                    _logger.LogDebug("CSS inlined successfully. Final HTML length: {Length} characters",
                        inlinedHtml.Length);
                }
                catch (Exception ex)
                {
                    var errorMsg = $"Failed to inline CSS: {ex.Message}";
                    _logger.LogError(ex, errorMsg);
                    result.ErrorMessage = errorMsg;
                    return result;
                }

                // Step 4: Send email via provider (Resend with SMTP fallback)
                try
                {
                    var sendRequest = new EmailSendRequest
                    {
                        To = new[] { model.RecipientEmail },
                        Subject = model.Subject,
                        HtmlBody = inlinedHtml,
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
