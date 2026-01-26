using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SnakeAid.Service.Implements.Email.Cores;
using SnakeAid.Service.Implements.Email.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SnakeAid.Service.Implements.Email.Providers
{
    /// <summary>
    /// Email sender using Resend HTTP API
    /// </summary>
    public class ResendEmailSender : IEmailSender
    {
        private readonly ILogger<ResendEmailSender> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly string _apiKey;
        private readonly string _endpoint;
        private readonly string _defaultFrom;

        public ResendEmailSender(
            ILogger<ResendEmailSender> logger,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;

            _apiKey = _configuration["EmailSettings:Resend:ApiKey"]
                ?? throw new InvalidOperationException("Resend API key not configured");
            _endpoint = _configuration["EmailSettings:Resend:Endpoint"] ?? "https://api.resend.com";
            _defaultFrom = _configuration["EmailSettings:DefaultFrom"]
                ?? throw new InvalidOperationException("Default from address not configured");

            _logger.LogInformation("ResendEmailSender initialized with endpoint: {Endpoint}", _endpoint);
        }

        public async Task<EmailSendResult> SendAsync(EmailSendRequest request, CancellationToken cancellationToken = default)
        {
            const int maxAttempts = 3;
            Exception? lastException = null;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    _logger.LogInformation("Attempting to send email via Resend (attempt {Attempt}/{Max})", attempt, maxAttempts);

                    using var client = _httpClientFactory.CreateClient();
                    client.BaseAddress = new Uri(_endpoint);
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

                    var payload = new
                    {
                        from = _defaultFrom,
                        to = request.To,
                        subject = request.Subject,
                        html = request.HtmlBody,
                        text = request.PlainTextBody,
                        cc = request.Cc,
                        bcc = request.Bcc,
                        reply_to = request.ReplyTo,
                        headers = request.Headers
                    };

                    var jsonContent = JsonSerializer.Serialize(payload);
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync("emails", content, cancellationToken);
                    var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                    if (response.IsSuccessStatusCode)
                    {
                        var result = JsonSerializer.Deserialize<ResendApiResponse>(responseBody);

                        _logger.LogInformation("Email sent successfully via Resend. Message ID: {MessageId}", result?.Id);

                        return new EmailSendResult
                        {
                            Success = true,
                            MessageId = result?.Id,
                            Provider = "Resend"
                        };
                    }

                    // Handle rate limiting (429) and server errors (5xx)
                    if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests ||
                        ((int)response.StatusCode >= 500 && (int)response.StatusCode < 600))
                    {
                        var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(Math.Pow(2, attempt));

                        if (attempt < maxAttempts)
                        {
                            _logger.LogWarning("Resend API returned {StatusCode}, retrying after {RetryAfter}s",
                                response.StatusCode, retryAfter.TotalSeconds);
                            await Task.Delay(retryAfter, cancellationToken);
                            continue;
                        }
                    }

                    // Parse error response
                    var errorResponse = JsonSerializer.Deserialize<ResendErrorResponse>(responseBody);
                    var errorMessage = errorResponse?.Message ?? $"HTTP {response.StatusCode}: {responseBody}";

                    _logger.LogError("Resend API error: {Error}", errorMessage);

                    return new EmailSendResult
                    {
                        Success = false,
                        ErrorMessage = errorMessage,
                        Provider = "Resend"
                    };
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    _logger.LogWarning(ex, "Resend send attempt {Attempt} failed", attempt);

                    if (attempt < maxAttempts)
                    {
                        var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                        await Task.Delay(delay, cancellationToken);
                    }
                }
            }

            _logger.LogError(lastException, "Failed to send email via Resend after {MaxAttempts} attempts", maxAttempts);

            return new EmailSendResult
            {
                Success = false,
                ErrorMessage = $"Failed after {maxAttempts} attempts: {lastException?.Message}",
                Provider = "Resend"
            };
        }

        private class ResendApiResponse
        {
            public string? Id { get; set; }
        }

        private class ResendErrorResponse
        {
            public string? Message { get; set; }
            public string? Name { get; set; }
        }
    }
}
