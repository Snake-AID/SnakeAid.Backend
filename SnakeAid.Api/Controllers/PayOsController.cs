using System.Text;
using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    private readonly ISnakeCatchingPaymentService _snakeCatchingPaymentService;
    private readonly ISnakebiteIncidentPaymentService _snakebiteIncidentPaymentService;
    private readonly IConsultationPaymentService _consultationPaymentService;
    private readonly IPaymentGateway _paymentGateway;
    private readonly PayOsDescriptionLookup _descriptionLookup;

    public PayOsController(
        ILogger<PayOsController> logger,
        IHttpContextAccessor httpContextAccessor,
        IMapper mapper,
        ISnakeCatchingPaymentService snakeCatchingPaymentService,
        ISnakebiteIncidentPaymentService snakebiteIncidentPaymentService,
        IConsultationPaymentService consultationPaymentService,
        IPaymentGateway paymentGateway,
        PayOsDescriptionLookup descriptionLookup)
        : base(logger, httpContextAccessor, mapper)
    {
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

            PayOsWebhookResponse result;
            if (description.StartsWith("CONSULTPAY-", StringComparison.Ordinal))
            {
                var consultResult = await _consultationPaymentService.ConfirmConsultationPaymentAsync(request.TransactionId, cancellationToken);
                result = new PayOsWebhookResponse { Success = true, Message = "Payment confirmed" };
            }
            else if (description.StartsWith("INCIDENT-", StringComparison.Ordinal))
                result = await _snakebiteIncidentPaymentService.ConfirmSnakebiteIncidentPaymentAsync(request.TransactionId, cancellationToken);
            else if (description.StartsWith("SNAKEAID-", StringComparison.Ordinal))
                result = await _snakeCatchingPaymentService.ConfirmSnakeCatchingPaymentAsync(request.TransactionId, cancellationToken);
            else
                return BadRequest(new { success = false, message = "Unknown payment flow for the given transaction" });

            return Ok(new { success = true, message = "PayOS payment confirmed successfully", data = result });
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
        Description = "Handles the return URL after user completes payment on PayOS portal. Automatically confirms payment if successful.",
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

            var isSuccess = code == "00" && status == "PAID" && !cancel;

            if (isSuccess)
            {
                try
                {
                    _logger.LogInformation("[PayOS Return] Payment successful, auto-confirming for orderCode={OrderCode}", orderCode);
                    await ConfirmByOrderCodeAsync(orderCode, cancellationToken);
                    _logger.LogInformation("[PayOS Return] Payment confirmed successfully. OrderCode={OrderCode}", orderCode);
                }
                catch (Exception confirmEx)
                {
                    _logger.LogError(confirmEx, "[PayOS Return] Failed to auto-confirm payment for orderCode={OrderCode}", orderCode);
                }
            }

            var resultHtml = BuildReturnHtml(isSuccess, orderCode, id, status);
            return Content(resultHtml, "text/html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PayOS return");
            return Content("<html><body><h1>Error processing payment return</h1></body></html>", "text/html");
        }
    }

    [AllowAnonymous]
    [HttpGet("cancel")]
    [SwaggerOperation(
        Summary = "PayOS cancel URL handler",
        Description = "Handles the cancel URL when user cancels payment on PayOS portal.",
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

            var cancelHtml = BuildCancelHtml(orderCode, id, status);
            return Content(cancelHtml, "text/html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PayOS cancel");
            return Content("<html><body><h1>Error processing payment cancellation</h1></body></html>", "text/html");
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

            PayOsWebhookResponse result;
            if (description != null && description.StartsWith("CONSULTPAY-", StringComparison.Ordinal))
                result = await _consultationPaymentService.ProcessConsultationWebhookAsync(rawPayload, cancellationToken);
            else if (description != null && description.StartsWith("INCIDENT-", StringComparison.Ordinal))
                result = await _snakebiteIncidentPaymentService.ProcessSnakebiteIncidentWebhookAsync(rawPayload, cancellationToken);
            else if (description != null && description.StartsWith("SNAKEAID-", StringComparison.Ordinal))
                result = await _snakeCatchingPaymentService.ProcessSnakeCatchingWebhookAsync(rawPayload, cancellationToken);
            else
            {
                _logger.LogWarning("No handler matched webhook description: {Description}", description);
                return BadRequest(new { success = false, message = "Unknown payment flow" });
            }

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

        if (description.StartsWith("CONSULTPAY-", StringComparison.Ordinal))
            await _consultationPaymentService.ConfirmConsultationPaymentByOrderCodeAsync(orderCode, ct);
        else if (description.StartsWith("INCIDENT-", StringComparison.Ordinal))
            await _snakebiteIncidentPaymentService.ConfirmSnakebiteIncidentPaymentByOrderCodeAsync(orderCode, ct);
        else if (description.StartsWith("SNAKEAID-", StringComparison.Ordinal))
            await _snakeCatchingPaymentService.ConfirmSnakeCatchingPaymentByOrderCodeAsync(orderCode, ct);
        else
            _logger.LogWarning("[PayOS] Unknown prefix in description for orderCode={OrderCode}", orderCode);
    }

    // ── HTML templates ──────────────────────────────────────────────────

    private static string BuildReturnHtml(bool isSuccess, long orderCode, string id, string status) => $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1'>
    <title>Payment {(isSuccess ? "Success" : "Failed")}</title>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; display: flex; justify-content: center; align-items: center; min-height: 100vh; margin: 0; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); }}
        .container {{ background: white; padding: 3rem; border-radius: 1rem; box-shadow: 0 20px 60px rgba(0,0,0,0.3); text-align: center; max-width: 500px; }}
        .icon {{ font-size: 4rem; margin-bottom: 1rem; }}
        h1 {{ color: #333; margin-bottom: 1rem; }}
        .details {{ background: #f5f5f5; padding: 1rem; border-radius: 0.5rem; margin: 1.5rem 0; text-align: left; }}
        .detail-row {{ display: flex; justify-content: space-between; padding: 0.5rem 0; border-bottom: 1px solid #ddd; }}
        .detail-row:last-child {{ border-bottom: none; }}
        .label {{ color: #666; font-weight: 500; }}
        .value {{ color: #333; font-weight: 600; }}
        button {{ background: #667eea; color: white; border: none; padding: 1rem 2rem; border-radius: 0.5rem; font-size: 1rem; cursor: pointer; margin-top: 1rem; }}
        button:hover {{ background: #5568d3; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='icon'>{(isSuccess ? "✅" : "❌")}</div>
        <h1>Payment {(isSuccess ? "Successful" : "Failed")}</h1>
        <p>{(isSuccess ? "Your payment has been processed successfully." : "Payment was not completed.")}</p>
        <div class='details'>
            <div class='detail-row'><span class='label'>Order Code:</span><span class='value'>{orderCode}</span></div>
            <div class='detail-row'><span class='label'>Transaction ID:</span><span class='value'>{id}</span></div>
            <div class='detail-row'><span class='label'>Status:</span><span class='value'>{status}</span></div>
        </div>
        <button onclick='window.close()'>Close Window</button>
    </div>
</body>
</html>";

    private static string BuildCancelHtml(long orderCode, string id, string status) => $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1'>
    <title>Payment Cancelled</title>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; display: flex; justify-content: center; align-items: center; min-height: 100vh; margin: 0; background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%); }}
        .container {{ background: white; padding: 3rem; border-radius: 1rem; box-shadow: 0 20px 60px rgba(0,0,0,0.3); text-align: center; max-width: 500px; }}
        .icon {{ font-size: 4rem; margin-bottom: 1rem; }}
        h1 {{ color: #333; margin-bottom: 1rem; }}
        p {{ color: #666; margin-bottom: 2rem; line-height: 1.6; }}
        .details {{ background: #f5f5f5; padding: 1rem; border-radius: 0.5rem; margin: 1.5rem 0; text-align: left; }}
        .detail-row {{ display: flex; justify-content: space-between; padding: 0.5rem 0; border-bottom: 1px solid #ddd; }}
        .detail-row:last-child {{ border-bottom: none; }}
        .label {{ color: #666; font-weight: 500; }}
        .value {{ color: #333; font-weight: 600; }}
        button {{ flex: 1; background: #6c757d; color: white; border: none; padding: 1rem 2rem; border-radius: 0.5rem; font-size: 1rem; cursor: pointer; }}
        button:hover {{ background: #5a6268; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='icon'>⚠️</div>
        <h1>Payment Cancelled</h1>
        <p>Your payment has been cancelled. The transaction was not completed.</p>
        <div class='details'>
            <div class='detail-row'><span class='label'>Order Code:</span><span class='value'>{orderCode}</span></div>
            <div class='detail-row'><span class='label'>Transaction ID:</span><span class='value'>{id}</span></div>
            <div class='detail-row'><span class='label'>Status:</span><span class='value'>{status}</span></div>
        </div>
        <button onclick='window.close()'>Close Window</button>
    </div>
</body>
</html>";
}
