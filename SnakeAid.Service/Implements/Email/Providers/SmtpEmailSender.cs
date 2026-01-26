using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SnakeAid.Service.Implements.Email.Cores;
using SnakeAid.Service.Implements.Email.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAid.Service.Implements.Email.Providers
{
    /// <summary>
    /// Email sender using SMTP protocol
    /// </summary>
    public class SmtpEmailSender : IEmailSender
    {
        private readonly ILogger<SmtpEmailSender> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly bool _enableSsl;
        private readonly string _defaultFrom;

        public SmtpEmailSender(
            ILogger<SmtpEmailSender> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;

            _smtpHost = _configuration["EmailSettings:SmtpHost"]
                ?? throw new InvalidOperationException("SMTP host not configured");
            _smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
            _smtpUsername = _configuration["EmailSettings:SmtpUsername"]
                ?? throw new InvalidOperationException("SMTP username not configured");
            _smtpPassword = _configuration["EmailSettings:SmtpPassword"]
                ?? throw new InvalidOperationException("SMTP password not configured");
            _enableSsl = bool.Parse(_configuration["EmailSettings:SmtpEnableSsl"] ?? "true");
            _defaultFrom = _configuration["EmailSettings:DefaultFrom"]
                ?? throw new InvalidOperationException("Default from address not configured");

            _logger.LogInformation("SmtpEmailSender initialized with host: {Host}:{Port}", _smtpHost, _smtpPort);
        }

        public async Task<EmailSendResult> SendAsync(EmailSendRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Sending email via SMTP to {Recipients}", string.Join(", ", request.To));

                using var smtpClient = new SmtpClient(_smtpHost, _smtpPort)
                {
                    EnableSsl = _enableSsl,
                    Credentials = new NetworkCredential(_smtpUsername, _smtpPassword),
                    Timeout = 30000 // 30 seconds
                };

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(_defaultFrom),
                    Subject = request.Subject,
                    Body = request.HtmlBody,
                    IsBodyHtml = true
                };

                // Add recipients
                foreach (var to in request.To)
                {
                    mailMessage.To.Add(to);
                }

                // Add CC
                if (request.Cc != null)
                {
                    foreach (var cc in request.Cc)
                    {
                        mailMessage.CC.Add(cc);
                    }
                }

                // Add BCC
                if (request.Bcc != null)
                {
                    foreach (var bcc in request.Bcc)
                    {
                        mailMessage.Bcc.Add(bcc);
                    }
                }

                // Add Reply-To
                if (!string.IsNullOrEmpty(request.ReplyTo))
                {
                    mailMessage.ReplyToList.Add(request.ReplyTo);
                }

                // Add custom headers
                if (request.Headers != null)
                {
                    foreach (var header in request.Headers)
                    {
                        mailMessage.Headers.Add(header.Key, header.Value);
                    }
                }

                // Add plain text alternative if provided
                if (!string.IsNullOrEmpty(request.PlainTextBody))
                {
                    var plainView = AlternateView.CreateAlternateViewFromString(request.PlainTextBody, null, "text/plain");
                    mailMessage.AlternateViews.Add(plainView);
                }

                await smtpClient.SendMailAsync(mailMessage, cancellationToken);

                _logger.LogInformation("Email sent successfully via SMTP");

                return new EmailSendResult
                {
                    Success = true,
                    Provider = "SMTP",
                    MessageId = mailMessage.Headers["Message-ID"]
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email via SMTP");

                return new EmailSendResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    Provider = "SMTP"
                };
            }
        }
    }
}
