using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Net.payOS;
using Net.payOS.Types;
using SnakeAid.Core.Settings;
using SnakeAid.Service.Interfaces;
using SnakeAid.Service.Services.PayOs.Models;

namespace SnakeAid.Service.Services.PayOs;

public class PayOsClient : IPayOsClient
{
    private readonly PayOS _client;
    private readonly PayOsOptions _options;
    private readonly ILogger<PayOsClient> _logger;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public PayOsClient(IOptions<PayOsOptions> options, ILogger<PayOsClient> logger)
    {
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
        _client = new PayOS(_options.ClientId, _options.ApiKey, _options.ChecksumKey);
    }

    public async Task<PayOsLinkCreated> CreatePaymentLinkAsync(PayOsLinkCreateContext context, CancellationToken cancellationToken)
    {
        var items = context.Items.Select(item => new ItemData(item.Name, item.Quantity, item.Price)).ToList();
        var paymentData = new PaymentData(
            orderCode: context.OrderCode,
            amount: context.Amount,
            description: context.Description,
            items: items,
            cancelUrl: context.CancelUrl,
            returnUrl: context.ReturnUrl);

        var result = await _client.createPaymentLink(paymentData).ConfigureAwait(false);

        return new PayOsLinkCreated
        {
            OrderCode = result.orderCode,
            PaymentLinkId = result.paymentLinkId,
            CheckoutUrl = result.checkoutUrl,
            Amount = result.amount,
            Status = result.status,
            Currency = result.currency
        };
    }

    public async Task<PayOsLinkInformation?> GetPaymentLinkInformationAsync(long orderCode, CancellationToken cancellationToken)
    {
        try
        {
            var info = await _client.getPaymentLinkInformation(orderCode).ConfigureAwait(false);
            return new PayOsLinkInformation
            {
                Id = info.id,
                OrderCode = info.orderCode,
                Amount = info.amount,
                AmountPaid = info.amountPaid,
                AmountRemaining = info.amountRemaining,
                Status = info.status
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get PayOS payment link info for orderCode {OrderCode}", orderCode);
            return null;
        }
    }

    public async Task<PayOsLinkInformation?> CancelPaymentLinkAsync(long orderCode, string? reason, CancellationToken cancellationToken)
    {
        try
        {
            var info = await _client.cancelPaymentLink(orderCode, reason).ConfigureAwait(false);
            return new PayOsLinkInformation
            {
                Id = info.id,
                OrderCode = info.orderCode,
                Amount = info.amount,
                AmountPaid = info.amountPaid,
                AmountRemaining = info.amountRemaining,
                Status = info.status
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel PayOS payment link for orderCode {OrderCode}", orderCode);
            return null;
        }
    }

    public async Task ConfirmWebhookAsync(string webhookUrl, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            return;
        }

        try
        {
            await _client.confirmWebhook(webhookUrl).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to confirm PayOS webhook URL {WebhookUrl}", webhookUrl);
        }
    }

    public PayOsWebhookData VerifyWebhook(string rawPayload)
    {
        if (string.IsNullOrWhiteSpace(rawPayload))
        {
            throw new ArgumentException("Webhook payload is empty", nameof(rawPayload));
        }

        var envelope = JsonSerializer.Deserialize<WebhookType>(rawPayload, _jsonSerializerOptions)
                       ?? throw new InvalidOperationException("Unable to deserialize PayOS webhook payload.");

        var data = _client.verifyPaymentWebhookData(envelope);

        return new PayOsWebhookData
        {
            Success = envelope.success && string.Equals(data.code, "00", StringComparison.OrdinalIgnoreCase),
            Code = data.code ?? envelope.code ?? string.Empty,
            Description = data.desc ?? envelope.desc ?? string.Empty,
            OrderCode = data.orderCode,
            Amount = data.amount,
            PaymentLinkId = data.paymentLinkId ?? string.Empty,
            TransactionReference = data.reference ?? string.Empty,
            TransactionDateTime = ParseDateTime(data.transactionDateTime),
            AccountNumber = data.accountNumber ?? string.Empty,
            AccountName = data.counterAccountName ?? string.Empty,
            CounterAccountBankName = data.counterAccountBankName ?? string.Empty,
            CounterAccountName = data.counterAccountName ?? string.Empty,
            CounterAccountNumber = data.counterAccountNumber ?? string.Empty,
            Signature = envelope.signature ?? string.Empty,
            RawPayload = rawPayload
        };
    }

    private static DateTime? ParseDateTime(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        if (DateTime.TryParse(
                input,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal,
                out var parsed))
        {
            return parsed.ToUniversalTime();
        }

        return null;
    }
}
