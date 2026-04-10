using System.Text;
using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Swashbuckle.AspNetCore.Annotations;
using SnakeAid.Core.Requests.PayOs;
using SnakeAid.Core.Responses.PayOs;
using SnakeAid.Service.Interfaces;
using SnakeAid.Service.Services.PayOs;

namespace SnakeAid.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class PayOsController : BaseController<PayOsController>
{
    private readonly IWalletTopupService _walletTopupService;
    private readonly ISnakeCatchingPaymentService _snakeCatchingPaymentService;
    private readonly ISnakebiteIncidentPaymentService _snakebiteIncidentPaymentService;
    private readonly IConsultationPaymentService _consultationPaymentService;
    private readonly IPaymentGateway _paymentGateway;
    private readonly PayOsDescriptionLookup _descriptionLookup;

    public PayOsController(
        ILogger<PayOsController> logger,
        IHttpContextAccessor httpContextAccessor,
        IMapper mapper,
        IWalletTopupService walletTopupService,
        ISnakeCatchingPaymentService snakeCatchingPaymentService,
        ISnakebiteIncidentPaymentService snakebiteIncidentPaymentService,
        IConsultationPaymentService consultationPaymentService,
        IPaymentGateway paymentGateway,
        PayOsDescriptionLookup descriptionLookup)
        : base(logger, httpContextAccessor, mapper)
    {
        _walletTopupService = walletTopupService;
        _snakeCatchingPaymentService = snakeCatchingPaymentService;
        _snakebiteIncidentPaymentService = snakebiteIncidentPaymentService;
        _consultationPaymentService = consultationPaymentService;
        _paymentGateway = paymentGateway;
        _descriptionLookup = descriptionLookup;
    }

    [HttpPost("confirm-payment")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Manually confirm PayOS payment",
        Description = "Fallback endpoint to confirm a PayOS payment when webhook delivery is unavailable.",
        Tags = new[] { "Payments" })]
    [ProducesResponseType(typeof(PayOsWebhookResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ConfirmPayment(
        [FromBody] ConfirmPaymentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request.TransactionId == Guid.Empty)
                return BadRequest(new { success = false, message = "TransactionId is required" });

            var description = await _descriptionLookup.GetByTransactionIdAsync(request.TransactionId, cancellationToken);
            if (description is null)
                return BadRequest(new { success = false, message = "Transaction not found" });

            if (!PayOsPaymentFlowPrefixes.TryResolve(description, out var flow))
                return BadRequest(new { success = false, message = "Unknown payment flow for the given transaction" });

            PayOsWebhookResponse? webhookResult = null;
            object? data = null;
            switch (flow)
            {
                case PayOsPaymentFlow.Topup:
                    webhookResult = await _walletTopupService.ConfirmWalletTopupAsync(request.TransactionId, cancellationToken);
                    data = webhookResult;
                    break;
                case PayOsPaymentFlow.Consultation:
                    var consultResult = await _consultationPaymentService.ConfirmConsultationPaymentAsync(request.TransactionId, cancellationToken);
                    data = consultResult;
                    break;
                case PayOsPaymentFlow.SnakebiteIncident:
                    webhookResult = await _snakebiteIncidentPaymentService.ConfirmSnakebiteIncidentPaymentAsync(request.TransactionId, cancellationToken);
                    data = webhookResult;
                    break;
                case PayOsPaymentFlow.SnakeCatching:
                    webhookResult = await _snakeCatchingPaymentService.ConfirmSnakeCatchingPaymentAsync(request.TransactionId, cancellationToken);
                    data = webhookResult;
                    break;
                default:
                    return BadRequest(new { success = false, message = "Unknown payment flow for the given transaction" });
            }

            var success = webhookResult?.Success ?? true;
            var message = webhookResult?.Message ?? "Payment confirmed successfully";
            return Ok(new { success, message, data });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when confirming payment");
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming payment for transaction {TransactionId}", request.TransactionId);
            return StatusCode(500, new { success = false, message = "An error occurred while confirming payment" });
        }
    }

    [AllowAnonymous]
    [HttpGet("return")]
    [SwaggerOperation(
        Summary = "PayOS return URL handler",
        Description = "Handles the return URL after user completes payment on PayOS portal, auto-confirms by orderCode, then redirects the user back into the mobile app via deep link.",
        Tags = new[] { "Payments" })]
    public async Task<IActionResult> Return(
        [FromQuery] string code,
        [FromQuery] string id,
        [FromQuery] bool cancel,
        [FromQuery] string status,
        [FromQuery] long orderCode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("[PayOS Return] code={Code}, id={Id}, cancel={Cancel}, status={Status}, orderCode={OrderCode}",
                code, id, cancel, status, orderCode);

            var payOsSuccess = code == "00" && status == "PAID" && !cancel;
            var redirectSuccess = payOsSuccess;
            string? reason = null;

            if (payOsSuccess)
            {
                try
                {
                    _logger.LogInformation("[PayOS Return] Payment successful, auto-confirming for orderCode={OrderCode}", orderCode);
                    await ConfirmByOrderCodeAsync(orderCode, cancellationToken);
                    _logger.LogInformation("[PayOS Return] Payment confirmed successfully. OrderCode={OrderCode}", orderCode);
                }
                catch (Exception confirmEx)
                {
                    redirectSuccess = false;
                    reason = "confirm_failed";
                    _logger.LogError(confirmEx, "[PayOS Return] Failed to auto-confirm payment for orderCode={OrderCode}", orderCode);
                }
            }

            return Redirect(BuildPaymentDeepLink(
                path: "return",
                success: redirectSuccess,
                orderCode: orderCode,
                id: id,
                status: status,
                cancel: cancel,
                reason: reason));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PayOS return");
            return Redirect(BuildPaymentDeepLink(
                path: "return",
                success: false,
                orderCode: orderCode,
                id: id,
                status: status,
                cancel: cancel,
                reason: "return_error"));
        }
    }

    [AllowAnonymous]
    [HttpGet("cancel")]
    [SwaggerOperation(
        Summary = "PayOS cancel URL handler",
        Description = "Handles the cancel URL when user cancels payment on PayOS portal and redirects the user back into the mobile app via deep link.",
        Tags = new[] { "Payments" })]
    public async Task<IActionResult> Cancel(
        [FromQuery] string code,
        [FromQuery] string id,
        [FromQuery] bool cancel,
        [FromQuery] string status,
        [FromQuery] long orderCode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("[PayOS Cancel] code={Code}, id={Id}, cancel={Cancel}, status={Status}, orderCode={OrderCode}",
                code, id, cancel, status, orderCode);

            return Redirect(BuildPaymentDeepLink(
                path: "cancel",
                success: false,
                orderCode: orderCode,
                id: id,
                status: status,
                cancel: true,
                reason: "cancelled"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PayOS cancel");
            return Redirect(BuildPaymentDeepLink(
                path: "cancel",
                success: false,
                orderCode: orderCode,
                id: id,
                status: status,
                cancel: true,
                reason: "cancel_error"));
        }
    }

    [AllowAnonymous]
    [HttpPost("webhook")]
    [SwaggerOperation(
        Summary = "PayOS webhook endpoint",
        Description = "Receives asynchronous PayOS notifications and updates payment statuses accordingly.",
        Tags = new[] { "Payments" })]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Webhook(CancellationToken cancellationToken)
    {
        try
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var rawPayload = await reader.ReadToEndAsync(cancellationToken);

            _logger.LogInformation("PayOS webhook received. Payload length: {Length}", rawPayload.Length);

            var webhookData = _paymentGateway.VerifyWebhook(rawPayload);
            var description = webhookData.Description;

            if (!PayOsPaymentFlowPrefixes.TryResolve(description, out var flow))
            {
                _logger.LogWarning("No handler matched webhook description: {Description}", description);
                return BadRequest(new { success = false, message = "Unknown payment flow" });
            }

            var result = flow switch
            {
                PayOsPaymentFlow.Topup => await _walletTopupService.ProcessWalletTopupWebhookAsync(rawPayload, cancellationToken),
                PayOsPaymentFlow.Consultation => await _consultationPaymentService.ProcessConsultationWebhookAsync(rawPayload, cancellationToken),
                PayOsPaymentFlow.SnakebiteIncident => await _snakebiteIncidentPaymentService.ProcessSnakebiteIncidentWebhookAsync(rawPayload, cancellationToken),
                PayOsPaymentFlow.SnakeCatching => await _snakeCatchingPaymentService.ProcessSnakeCatchingWebhookAsync(rawPayload, cancellationToken),
                _ => throw new InvalidOperationException("Unknown PayOS payment flow.")
            };

            _logger.LogInformation("Webhook processed. Success={Success} OrderCode={OrderCode}", result.Success, result.OrderCode);
            return Ok(new { success = result.Success, message = result.Message, data = result });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid webhook payload");
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PayOS webhook");
            return StatusCode(500, new { success = false, message = "An error occurred while processing webhook" });
        }
    }

    // ── Private helpers: prefix dispatch ────────────────────────────────

    private async Task ConfirmByOrderCodeAsync(long orderCode, CancellationToken ct)
    {
        var description = await _descriptionLookup.GetByOrderCodeAsync(orderCode, ct);
        if (description is null)
        {
            _logger.LogWarning("[PayOS] No transaction found for orderCode={OrderCode}", orderCode);
            return;
        }

        if (!PayOsPaymentFlowPrefixes.TryResolve(description, out var flow))
        {
            _logger.LogWarning("[PayOS] Unknown prefix in description for orderCode={OrderCode}", orderCode);
            return;
        }

        switch (flow)
        {
            case PayOsPaymentFlow.Topup:
                await _walletTopupService.ConfirmWalletTopupByOrderCodeAsync(orderCode, ct);
                break;
            case PayOsPaymentFlow.Consultation:
                await _consultationPaymentService.ConfirmConsultationPaymentByOrderCodeAsync(orderCode, ct);
                break;
            case PayOsPaymentFlow.SnakebiteIncident:
                await _snakebiteIncidentPaymentService.ConfirmSnakebiteIncidentPaymentByOrderCodeAsync(orderCode, ct);
                break;
            case PayOsPaymentFlow.SnakeCatching:
                await _snakeCatchingPaymentService.ConfirmSnakeCatchingPaymentByOrderCodeAsync(orderCode, ct);
                break;
            default:
                _logger.LogWarning("[PayOS] Unknown prefix in description for orderCode={OrderCode}", orderCode);
                break;
        }
    }

    private static string BuildPaymentDeepLink(
        string path,
        bool success,
        long orderCode,
        string? id,
        string? status,
        bool cancel,
        string? reason)
    {
        var parameters = new Dictionary<string, string?>
        {
            ["success"] = success ? "true" : "false",
            ["orderCode"] = orderCode.ToString(),
            ["status"] = status,
            ["cancel"] = cancel ? "true" : "false",
            ["id"] = string.IsNullOrWhiteSpace(id) ? null : id,
            ["reason"] = string.IsNullOrWhiteSpace(reason) ? null : reason
        };

        return QueryHelpers.AddQueryString(
            $"snakeaid://payment/{path}",
            parameters
                .Where(x => !string.IsNullOrWhiteSpace(x.Value))
                .ToDictionary(x => x.Key, x => (string?)x.Value));
    }
}
