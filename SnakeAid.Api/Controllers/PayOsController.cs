using System.Text;
using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using SnakeAid.Core.Requests.PayOs;
using SnakeAid.Core.Responses.PayOs;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class PayOsController : BaseController<PayOsController>
{
    private readonly ISnakeCatchingPaymentService _snakeCatchingPaymentService;
    private readonly IConsultationPaymentService _consultationPaymentService;
    private readonly IPaymentGateway _paymentGateway;

    public PayOsController(
        ILogger<PayOsController> logger,
        IHttpContextAccessor httpContextAccessor,
        IMapper mapper,
        ISnakeCatchingPaymentService snakeCatchingPaymentService,
        IConsultationPaymentService consultationPaymentService,
        IPaymentGateway paymentGateway)
        : base(logger, httpContextAccessor, mapper)
    {
        _snakeCatchingPaymentService = snakeCatchingPaymentService;
        _consultationPaymentService = consultationPaymentService;
        _paymentGateway = paymentGateway;
    }

    [HttpPost("snakecatching/paylink/create")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Create PayOS payment link",
        Description = "Generates a PayOS payment link for a snake catching request that needs payment. Sender is current user, receiver is system account.",
        Tags = new[] { "Payments" })]
    [ProducesResponseType(typeof(SnakeCatchingPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateSnakeCatchingPaymentLink(
        [FromBody] CreateSnakeCatchingPaymentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var result = await _snakeCatchingPaymentService.CreateSnakeCatchingPaymentLinkAsync(request, currentUserId, cancellationToken);

            return Ok(new
            {
                success = true,
                message = "PayOS payment link created successfully",
                data = result
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when creating payment link");
            return BadRequest(new
            {
                success = false,
                message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment link");
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while creating payment link"
            });
        }
    }

    [HttpPost("snakecatching/paylink/cancel/{orderCode}")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Cancel PayOS payment link",
        Description = "Cancels the PayOS payment link using the original order code.",
        Tags = new[] { "Payments" })]
    [ProducesResponseType(typeof(CancelPaymentLinkResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CancelPaymentLink(
        [FromRoute] long orderCode,
        [FromBody] CancelPaymentLinkRequest? request,
        CancellationToken cancellationToken)
    {
        try
        {
            var payload = request ?? new CancelPaymentLinkRequest();
            var result = await _snakeCatchingPaymentService.CancelSnakeCatchingPaymentLinkAsync(orderCode, payload, cancellationToken);
            return Ok(new
            {
                success = true,
                message = "PayOS payment link cancelled successfully",
                data = result
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when cancelling payment link");
            return BadRequest(new
            {
                success = false,
                message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling payment link for orderCode {OrderCode}", orderCode);
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while cancelling payment link"
            });
        }
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
            {
                return BadRequest(new
                {
                    success = false,
                    message = "TransactionId is required"
                });
            }

            var result = await _snakeCatchingPaymentService.ConfirmSnakeCatchingPaymentAsync(request.TransactionId, cancellationToken);
            return Ok(new
            {
                success = true,
                message = "PayOS payment confirmed successfully",
                data = result
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when confirming payment");
            return BadRequest(new
            {
                success = false,
                message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming payment for transaction {TransactionId}", request.TransactionId);
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while confirming payment"
            });
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

            // Auto-confirm payment if successful
            if (isSuccess)
            {
                try
                {
                    _logger.LogInformation("[PayOS Return] Payment successful, auto-confirming for orderCode={OrderCode}", orderCode);

                    var isConsultationOrder = await _consultationPaymentService.IsConsultationPayOsOrderCodeAsync(orderCode, cancellationToken);
                    var confirmResult = isConsultationOrder
                        ? await _consultationPaymentService.ConfirmConsultationPaymentByOrderCodeAsync(orderCode, cancellationToken)
                        : await _snakeCatchingPaymentService.ConfirmSnakeCatchingPaymentByOrderCodeAsync(orderCode, cancellationToken);
                    
                    _logger.LogInformation("[PayOS Return] Payment confirmed successfully. OrderCode={OrderCode}, Success={Success}", 
                        orderCode, confirmResult.Success);
                }
                catch (Exception confirmEx)
                {
                    _logger.LogError(confirmEx, "[PayOS Return] Failed to auto-confirm payment for orderCode={OrderCode}", orderCode);
                    // Don't throw - still show success page to user
                }
            }
            
            // Return a simple HTML page with payment result
            var resultHtml = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1'>
    <title>Payment {(isSuccess ? "Success" : "Failed")}</title>
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, sans-serif;
            display: flex;
            justify-content: center;
            align-items: center;
            min-height: 100vh;
            margin: 0;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
        }}
        .container {{
            background: white;
            padding: 3rem;
            border-radius: 1rem;
            box-shadow: 0 20px 60px rgba(0,0,0,0.3);
            text-align: center;
            max-width: 500px;
        }}
        .icon {{
            font-size: 4rem;
            margin-bottom: 1rem;
        }}
        h1 {{
            color: #333;
            margin-bottom: 1rem;
        }}
        .details {{
            background: #f5f5f5;
            padding: 1rem;
            border-radius: 0.5rem;
            margin: 1.5rem 0;
            text-align: left;
        }}
        .detail-row {{
            display: flex;
            justify-content: space-between;
            padding: 0.5rem 0;
            border-bottom: 1px solid #ddd;
        }}
        .detail-row:last-child {{
            border-bottom: none;
        }}
        .label {{
            color: #666;
            font-weight: 500;
        }}
        .value {{
            color: #333;
            font-weight: 600;
        }}
        button {{
            background: #667eea;
            color: white;
            border: none;
            padding: 1rem 2rem;
            border-radius: 0.5rem;
            font-size: 1rem;
            cursor: pointer;
            margin-top: 1rem;
            transition: background 0.3s;
        }}
        button:hover {{
            background: #5568d3;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='icon'>{(isSuccess ? "✅" : "❌")}</div>
        <h1>Payment {(isSuccess ? "Successful" : "Failed")}</h1>
        <p>{(isSuccess ? "Your payment has been processed successfully." : "Payment was not completed.")}</p>
        
        <div class='details'>
            <div class='detail-row'>
                <span class='label'>Order Code:</span>
                <span class='value'>{orderCode}</span>
            </div>
            <div class='detail-row'>
                <span class='label'>Transaction ID:</span>
                <span class='value'>{id}</span>
            </div>
            <div class='detail-row'>
                <span class='label'>Status:</span>
                <span class='value'>{status}</span>
            </div>
        </div>
        
        <button onclick='window.close()'>Close Window</button>
    </div>
</body>
</html>";

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
    public IActionResult Cancel(
        [FromQuery] string code,
        [FromQuery] string id,
        [FromQuery] bool cancel,
        [FromQuery] string status,
        [FromQuery] long orderCode)
    {
        try
        {
            _logger.LogInformation("[PayOS Cancel] code={Code}, id={Id}, cancel={Cancel}, status={Status}, orderCode={OrderCode}", 
                code, id, cancel, status, orderCode);

            // Return a simple HTML page showing cancellation
            var cancelHtml = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1'>
    <title>Payment Cancelled</title>
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, sans-serif;
            display: flex;
            justify-content: center;
            align-items: center;
            min-height: 100vh;
            margin: 0;
            background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%);
        }}
        .container {{
            background: white;
            padding: 3rem;
            border-radius: 1rem;
            box-shadow: 0 20px 60px rgba(0,0,0,0.3);
            text-align: center;
            max-width: 500px;
        }}
        .icon {{
            font-size: 4rem;
            margin-bottom: 1rem;
        }}
        h1 {{
            color: #333;
            margin-bottom: 1rem;
        }}
        p {{
            color: #666;
            margin-bottom: 2rem;
            line-height: 1.6;
        }}
        .details {{
            background: #f5f5f5;
            padding: 1rem;
            border-radius: 0.5rem;
            margin: 1.5rem 0;
            text-align: left;
        }}
        .detail-row {{
            display: flex;
            justify-content: space-between;
            padding: 0.5rem 0;
            border-bottom: 1px solid #ddd;
        }}
        .detail-row:last-child {{
            border-bottom: none;
        }}
        .label {{
            color: #666;
            font-weight: 500;
        }}
        .value {{
            color: #333;
            font-weight: 600;
        }}
        .button-group {{
            display: flex;
            gap: 1rem;
            margin-top: 1.5rem;
        }}
        button {{
            flex: 1;
            color: white;
            border: none;
            padding: 1rem 2rem;
            border-radius: 0.5rem;
            font-size: 1rem;
            cursor: pointer;
            transition: all 0.3s;
        }}
        .btn-close {{
            background: #6c757d;
        }}
        .btn-close:hover {{
            background: #5a6268;
        }}
        .btn-retry {{
            background: #667eea;
        }}
        .btn-retry:hover {{
            background: #5568d3;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='icon'>⚠️</div>
        <h1>Payment Cancelled</h1>
        <p>Your payment has been cancelled. The transaction was not completed.</p>
        
        <div class='details'>
            <div class='detail-row'>
                <span class='label'>Order Code:</span>
                <span class='value'>{orderCode}</span>
            </div>
            <div class='detail-row'>
                <span class='label'>Transaction ID:</span>
                <span class='value'>{id}</span>
            </div>
            <div class='detail-row'>
                <span class='label'>Status:</span>
                <span class='value'>{status}</span>
            </div>
        </div>
        
        <div class='button-group'>
            <button class='btn-close' onclick='window.close()'>Close Window</button>
        </div>
    </div>
</body>
</html>";

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
            var rawPayload = await reader.ReadToEndAsync();

            _logger.LogInformation("PayOS webhook received. Payload length: {Length}", rawPayload.Length);

            var webhook = _paymentGateway.VerifyWebhook(rawPayload);
            var isConsultationOrder = await _consultationPaymentService.IsConsultationPayOsOrderCodeAsync(webhook.OrderCode, cancellationToken);
            var result = isConsultationOrder
                ? await _consultationPaymentService.ProcessConsultationWebhookAsync(rawPayload, cancellationToken)
                : await _snakeCatchingPaymentService.ProcessSnakeCatchingWebhookAsync(rawPayload, cancellationToken);

            return Ok(new
            {
                success = result.Success,
                message = result.Message,
                data = result
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid webhook payload");
            return BadRequest(new
            {
                success = false,
                message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PayOS webhook");
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while processing webhook"
            });
        }
    }

    [HttpPost("transfer-to-rescuer")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Transfer funds to rescuer",
        Description = "Transfers all paid funds for a catching request from system wallet to the assigned rescuer's wallet.",
        Tags = new[] { "Payments" })]
    [ProducesResponseType(typeof(TransferToRescuerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> TransferToRescuer(
        [FromBody] TransferToRescuerRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _snakeCatchingPaymentService.TransferSnakeCatchingFundsToRescuerAsync(request, cancellationToken);
            return Ok(new
            {
                success = true,
                message = "Funds transferred successfully to rescuer",
                data = result
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when transferring to rescuer");
            return BadRequest(new
            {
                success = false,
                message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transferring funds to rescuer");
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while transferring funds"
            });
        }
    }
}
