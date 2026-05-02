using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using SnakeAid.Core.Meta;
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
    [ProducesResponseType(typeof(ApiResponse<SnakeCatchingPaymentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePaymentLink(
        [FromBody] CreateSnakeCatchingPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var result = await _snakeCatchingPaymentService.CreateSnakeCatchingPaymentLinkAsync(request, currentUserId, cancellationToken);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "PayOS payment link created successfully"));
    }

    [HttpPost("wallet")]
    [SwaggerOperation(
        Summary = "Create wallet payment",
        Description = "Process snake catching payment using the caller's wallet balance.",
        Tags = new[] { "Snake Catching Payments" })]
    [ProducesResponseType(typeof(ApiResponse<SnakeCatchingPaymentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateWalletPayment(
        [FromBody] CreateSnakeCatchingPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var result = await _snakeCatchingPaymentService.CreateWalletPaymentAsync(request, currentUserId, cancellationToken);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Snake catching wallet payment processed successfully"));
    }

    [HttpPost("cancel-link/{orderCode}")]
    [SwaggerOperation(
        Summary = "Cancel PayOS payment link",
        Description = "Cancels the PayOS payment link using the original order code.",
        Tags = new[] { "Snake Catching Payments" })]
    [ProducesResponseType(typeof(ApiResponse<CancelPaymentLinkResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelPaymentLink(
        [FromRoute] long orderCode,
        [FromBody] CancelPaymentLinkRequest? request,
        CancellationToken cancellationToken)
    {
        var payload = request ?? new CancelPaymentLinkRequest();
        var result = await _snakeCatchingPaymentService.CancelSnakeCatchingPaymentLinkAsync(orderCode, payload, cancellationToken);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "PayOS payment link cancelled successfully"));
    }

    [HttpPost("transfer-to-rescuer")]
    [SwaggerOperation(
        Summary = "Transfer funds to rescuer",
        Description = "Deprecated compatibility endpoint. Snake catching customer payment is recorded as system revenue; no rescuer transfer is performed.",
        Tags = new[] { "Snake Catching Payments" })]
    [ProducesResponseType(typeof(ApiResponse<TransferToRescuerResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TransferToRescuer(
        [FromBody] TransferToRescuerRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _snakeCatchingPaymentService.TransferSnakeCatchingFundsToRescuerAsync(request, cancellationToken);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Funds transferred successfully to rescuer"));
    }

    [HttpPost("refund")]
    [SwaggerOperation(
        Summary = "Refund snake catching payment",
        Description = "Refund a snake catching payment to the user's wallet and create a refund transaction.",
        Tags = new[] { "Snake Catching Payments" })]
    [ProducesResponseType(typeof(ApiResponse<SnakeCatchingRefundResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RefundSnakeCatching(
        [FromBody] SnakeCatchingRefundRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _snakeCatchingPaymentService.RefundSnakeCatchingAsync(request, cancellationToken);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Snake catching refund processed successfully"));
    }
}
