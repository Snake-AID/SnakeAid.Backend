using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using SnakeAid.Core.Requests.PayOs;
using SnakeAid.Core.Responses.PayOs;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Api.Controllers;

[ApiController]
[Route("api/snakecatching/payment")]
[Authorize]
public class SnakeCatchingPaymentsController : BaseController<SnakeCatchingPaymentsController>
{
    private readonly ISnakeCatchingPaymentService _snakeCatchingPaymentService;

    public SnakeCatchingPaymentsController(
        ILogger<SnakeCatchingPaymentsController> logger,
        IHttpContextAccessor httpContextAccessor,
        IMapper mapper,
        ISnakeCatchingPaymentService snakeCatchingPaymentService)
        : base(logger, httpContextAccessor, mapper)
    {
        _snakeCatchingPaymentService = snakeCatchingPaymentService;
    }

    [HttpPost("create-link")]
    [SwaggerOperation(
        Summary = "Create PayOS payment link",
        Description = "Generates a PayOS payment link for a snake catching request.",
        Tags = new[] { "Snake Catching Payments" })]
    [ProducesResponseType(typeof(SnakeCatchingPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePaymentLink(
        [FromBody] CreateSnakeCatchingPaymentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var result = await _snakeCatchingPaymentService.CreateSnakeCatchingPaymentLinkAsync(request, currentUserId, cancellationToken);
            return Ok(new { success = true, message = "PayOS payment link created successfully", data = result });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when creating payment link");
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment link");
            return StatusCode(500, new { success = false, message = "An error occurred while creating payment link" });
        }
    }

    [HttpPost("cancel-link/{orderCode}")]
    [SwaggerOperation(
        Summary = "Cancel PayOS payment link",
        Description = "Cancels the PayOS payment link using the original order code.",
        Tags = new[] { "Snake Catching Payments" })]
    [ProducesResponseType(typeof(CancelPaymentLinkResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelPaymentLink(
        [FromRoute] long orderCode,
        [FromBody] CancelPaymentLinkRequest? request,
        CancellationToken cancellationToken)
    {
        try
        {
            var payload = request ?? new CancelPaymentLinkRequest();
            var result = await _snakeCatchingPaymentService.CancelSnakeCatchingPaymentLinkAsync(orderCode, payload, cancellationToken);
            return Ok(new { success = true, message = "PayOS payment link cancelled successfully", data = result });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when cancelling payment link");
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling payment link for orderCode {OrderCode}", orderCode);
            return StatusCode(500, new { success = false, message = "An error occurred while cancelling payment link" });
        }
    }

    [HttpPost("transfer-to-rescuer")]
    [SwaggerOperation(
        Summary = "Transfer funds to rescuer",
        Description = "Transfers paid funds for a catching request from system wallet to the assigned rescuer's wallet.",
        Tags = new[] { "Snake Catching Payments" })]
    [ProducesResponseType(typeof(TransferToRescuerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TransferToRescuer(
        [FromBody] TransferToRescuerRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _snakeCatchingPaymentService.TransferSnakeCatchingFundsToRescuerAsync(request, cancellationToken);
            return Ok(new { success = true, message = "Funds transferred successfully to rescuer", data = result });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when transferring to rescuer");
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transferring funds to rescuer");
            return StatusCode(500, new { success = false, message = "An error occurred while transferring funds" });
        }
    }
}
