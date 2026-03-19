using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SnakeAid.Core.Settings;
using SnakeAid.Service.Interfaces;
using SnakeAid.Service.Services.PayOs.Models;

namespace SnakeAid.Service.Services.PayOs;

public class PayOsProvider : IPayOsProvider
{
    private readonly IPayOsClient _payOsClient;
    private readonly PayOsOptions _options;
    private readonly ILogger<PayOsProvider> _logger;

    public PayOsProvider(
        IPayOsClient payOsClient,
        IOptions<PayOsOptions> options,
        ILogger<PayOsProvider> logger)
    {
        _payOsClient = payOsClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<PayOsPaymentLinkResult> CreatePaymentLinkAsync(
        PayOsCreatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("[PayOsProvider] Creating payment link for OrderCode {OrderCode}, Amount {Amount}",
                request.OrderCode, request.Amount);

            var amountInt = Convert.ToInt32(Math.Round(request.Amount, MidpointRounding.AwayFromZero));

            var payOsRequest = new PayOsLinkCreateContext
            {
                OrderCode = request.OrderCode,
                Amount = amountInt,
                Description = request.Description,
                ReturnUrl = _options.ReturnUrl,
                CancelUrl = _options.CancelUrl,
                Items = new[]
                {
                    new PayOsItemPayload
                    {
                        Name = request.ItemName,
                        Quantity = request.Quantity,
                        Price = amountInt
                    }
                }
            };

            var result = await _payOsClient.CreatePaymentLinkAsync(payOsRequest, cancellationToken);

            _logger.LogInformation("[PayOsProvider] Payment link created successfully. OrderCode {OrderCode}, CheckoutUrl {CheckoutUrl}",
                request.OrderCode, result.CheckoutUrl);

            return new PayOsPaymentLinkResult
            {
                OrderCode = result.OrderCode,
                PaymentLinkId = result.PaymentLinkId,
                CheckoutUrl = result.CheckoutUrl,
                Amount = result.Amount,
                Status = result.Status,
                Currency = result.Currency,
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PayOsProvider] Failed to create payment link for OrderCode {OrderCode}", request.OrderCode);

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
            _logger.LogInformation("[PayOsProvider] Cancelling payment link for OrderCode {OrderCode}", orderCode);

            var result = await _payOsClient.CancelPaymentLinkAsync(orderCode, cancellationReason, cancellationToken);

            if (result == null)
            {
                return new PayOsPaymentLinkResult
                {
                    OrderCode = orderCode,
                    Success = false,
                    ErrorMessage = "Payment link not found or could not be cancelled"
                };
            }

            _logger.LogInformation("[PayOsProvider] Payment link cancelled successfully. OrderCode {OrderCode}", orderCode);

            return new PayOsPaymentLinkResult
            {
                OrderCode = orderCode,
                Amount = result.Amount,
                AmountPaid = result.AmountPaid,
                AmountRemaining = result.AmountRemaining,
                Status = result.Status,
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PayOsProvider] Failed to cancel payment link for OrderCode {OrderCode}", orderCode);

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
            _logger.LogInformation("[PayOsProvider] Getting payment link information for OrderCode {OrderCode}", orderCode);

            var result = await _payOsClient.GetPaymentLinkInformationAsync(orderCode, cancellationToken);

            if (result != null)
            {
                _logger.LogInformation("[PayOsProvider] Payment link information retrieved. OrderCode {OrderCode}, Status {Status}",
                    orderCode, result.Status);
            }
            else
            {
                _logger.LogWarning("[PayOsProvider] Payment link not found for OrderCode {OrderCode}", orderCode);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PayOsProvider] Failed to get payment link information for OrderCode {OrderCode}", orderCode);
            throw;
        }
    }

    public PayOsWebhookData VerifyWebhook(string rawPayload)
    {
        try
        {
            _logger.LogInformation("[PayOsProvider] Verifying webhook payload");

            var webhookData = _payOsClient.VerifyWebhook(rawPayload);

            _logger.LogInformation("[PayOsProvider] Webhook verified successfully. OrderCode {OrderCode}, Success {Success}",
                webhookData.OrderCode, webhookData.Success);

            return webhookData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PayOsProvider] Failed to verify webhook payload");
            throw;
        }
    }
}