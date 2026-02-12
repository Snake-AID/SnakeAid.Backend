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
    private readonly IPayOsPaymentService _payOsPaymentService;

    public PayOsController(
        ILogger<PayOsController> logger,
        IHttpContextAccessor httpContextAccessor,
        IMapper mapper,
        IPayOsPaymentService payOsPaymentService)
        : base(logger, httpContextAccessor, mapper)
    {
        _payOsPaymentService = payOsPaymentService;
    }

    /// <summary>
    /// Create PayOS payment link for snake catching service
    /// </summary>
    /// <param name="request">Payment request with sender, receiver, and amount</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment link response with checkout URL</returns>
    [HttpPost("create-payment-link")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Create PayOS payment link",
        Description = "Generates a PayOS payment link for a snake catching request that needs payment.",
        Tags = new[] { "Payments" })]
    [ProducesResponseType(typeof(SnakeCatchingPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreatePaymentLink(
        [FromBody] CreateSnakeCatchingPaymentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _payOsPaymentService.CreatePaymentLinkAsync(request, cancellationToken);
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

    /// <summary>
    /// Cancel PayOS payment link
    /// </summary>
    /// <param name="orderCode">PayOS order code</param>
    /// <param name="request">Cancellation details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cancellation confirmation</returns>
    [HttpPost("cancel-payment-link/{orderCode}")]
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
            var result = await _payOsPaymentService.CancelPaymentLinkAsync(orderCode, payload, cancellationToken);
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

    /// <summary>
    /// Manually confirm PayOS payment
    /// </summary>
    /// <param name="request">Transaction ID to confirm</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment confirmation result</returns>
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

            var result = await _payOsPaymentService.ConfirmPaymentAsync(request.TransactionId, cancellationToken);
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

    /// <summary>
    /// PayOS return URL handler
    /// </summary>
    /// <param name="code">PayOS response code (00 = success)</param>
    /// <param name="id">PayOS transaction ID</param>
    /// <param name="cancel">Whether payment was cancelled</param>
    /// <param name="status">Payment status (PAID, CANCELLED, etc.)</param>
    /// <param name="orderCode">Order code</param>
    /// <returns>Payment result page</returns>
    [AllowAnonymous]
    [HttpGet("return")]
    [SwaggerOperation(
        Summary = "PayOS return URL handler",
        Description = "Handles the return URL after user completes payment on PayOS portal.",
        Tags = new[] { "Payments" })]
    public async Task<IActionResult> Return(
        [FromQuery] string code,
        [FromQuery] string id,
        [FromQuery] bool cancel,
        [FromQuery] string status,
        [FromQuery] long orderCode)
    {
        try
        {
            _logger.LogInformation("[PayOS Return] code={Code}, id={Id}, cancel={Cancel}, status={Status}, orderCode={OrderCode}", 
                code, id, cancel, status, orderCode);

            var isSuccess = code == "00" && status == "PAID" && !cancel;
            
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

    /// <summary>
    /// PayOS webhook endpoint
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Webhook processing result</returns>
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

            var result = await _payOsPaymentService.ProcessWebhookAsync(rawPayload, cancellationToken);

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
}
