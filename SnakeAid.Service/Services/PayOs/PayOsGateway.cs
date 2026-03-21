using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Net.payOS;
using Net.payOS.Types;
using SnakeAid.Core.Settings;
using SnakeAid.Service.Interfaces;
using SnakeAid.Service.Services.PayOs.Models;

namespace SnakeAid.Service.Services.PayOs;

public class PayOsGateway : IPaymentGateway
{
    private readonly PayOS _client;
    private readonly PayOsOptions _options;
    private readonly ILogger<PayOsGateway> _logger;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public PayOsGateway(IOptions<PayOsOptions> options, ILogger<PayOsGateway> logger)
    {
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
        _client = new PayOS(_options.ClientId, _options.ApiKey, _options.ChecksumKey);
    }

    public async Task<PayOsPaymentLinkResult> CreatePaymentLinkAsync(
        PayOsCreatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("[PayOsGateway] Creating payment link for OrderCode {OrderCode}, Amount {Amount}",
                request.OrderCode, request.Amount);

            var amountInt = Convert.ToInt32(Math.Round(request.Amount, MidpointRounding.AwayFromZero));
            var items = new List<ItemData>
            {
                new(request.ItemName, request.Quantity, amountInt)
            };

            var paymentData = new PaymentData(
                orderCode: request.OrderCode,
                amount: amountInt,
                description: request.Description,
                items: items,
                cancelUrl: _options.CancelUrl,
                returnUrl: _options.ReturnUrl);

            var result = await _client.createPaymentLink(paymentData).ConfigureAwait(false);

            _logger.LogInformation("[PayOsGateway] Payment link created successfully. OrderCode {OrderCode}, CheckoutUrl {CheckoutUrl}",
                request.OrderCode, result.checkoutUrl);

            return new PayOsPaymentLinkResult
            {
                OrderCode = result.orderCode,
                PaymentLinkId = result.paymentLinkId,
                CheckoutUrl = result.checkoutUrl,
                Amount = result.amount,
                Status = result.status,
                Currency = result.currency,
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PayOsGateway] Failed to create payment link for OrderCode {OrderCode}", request.OrderCode);

            return new PayOsPaymentLinkResult
            {
                OrderCode = request.OrderCode,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<PayOsPaymentLinkResult> CancelPaymentLinkAsync(
        long orderCode,
        string? cancellationReason,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("[PayOsGateway] Cancelling payment link for OrderCode {OrderCode}", orderCode);

            var result = await _client.cancelPaymentLink(orderCode, cancellationReason).ConfigureAwait(false);

            _logger.LogInformation("[PayOsGateway] Payment link cancelled successfully. OrderCode {OrderCode}", orderCode);

            return new PayOsPaymentLinkResult
            {
                OrderCode = orderCode,
                Amount = result.amount,
                AmountPaid = result.amountPaid,
                AmountRemaining = result.amountRemaining,
                Status = result.status,
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PayOsGateway] Failed to cancel payment link for OrderCode {OrderCode}", orderCode);

            return new PayOsPaymentLinkResult
            {
                OrderCode = orderCode,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<PayOsLinkInformation?> GetPaymentLinkInformationAsync(
        long orderCode,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("[PayOsGateway] Getting payment link information for OrderCode {OrderCode}", orderCode);

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
            _logger.LogError(ex, "[PayOsGateway] Failed to get payment link information for OrderCode {OrderCode}", orderCode);
            return null;
        }
    }

    public PayOsWebhookData VerifyWebhook(string rawPayload)
    {
        if (string.IsNullOrWhiteSpace(rawPayload))
        {
            throw new ArgumentException("Webhook payload is empty", nameof(rawPayload));
        }

        try
        {
            _logger.LogInformation("[PayOsGateway] Verifying webhook payload");

            var envelope = JsonSerializer.Deserialize<WebhookType>(rawPayload, _jsonSerializerOptions)
                           ?? throw new InvalidOperationException("Unable to deserialize PayOS webhook payload.");

            var data = _client.verifyPaymentWebhookData(envelope);

            var webhookData = new PayOsWebhookData
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

            _logger.LogInformation("[PayOsGateway] Webhook verified successfully. OrderCode {OrderCode}, Success {Success}",
                webhookData.OrderCode, webhookData.Success);

            return webhookData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PayOsGateway] Failed to verify webhook payload");
            throw;
        }
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
