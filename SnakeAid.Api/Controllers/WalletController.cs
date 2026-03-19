using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.PayOs;
using SnakeAid.Core.Responses.PayOs;
using SnakeAid.Core.Requests.Wallet;
using SnakeAid.Core.Responses.Wallet;
using SnakeAid.Service.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SnakeAid.Api.Controllers
{
    [Route("api/wallet")]
    [ApiController]
    [Authorize]
    public class WalletController : BaseController<WalletController>
    {
        private readonly IWalletService _walletService;
        private readonly IWalletPaymentService _walletPaymentService;

        public WalletController(
            ILogger<WalletController> logger,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            IWalletService walletService,
            IWalletPaymentService walletPaymentService)
            : base(logger, httpContextAccessor, mapper)
        {
            _walletService = walletService;
            _walletPaymentService = walletPaymentService;
        }

        /// <summary>
        /// Get current user's wallet details
        /// </summary>
        [HttpGet("me")]
        [SwaggerOperation(
            Summary = "Get My Wallet Details",
            Description = "Retrieve wallet details (balance, etc.) for the currently authenticated user")]
        [SwaggerResponse(200, "Wallet retrieved successfully", typeof(ApiResponse<WalletResponse>))]
        [SwaggerResponse(404, "Wallet not found for the current user")]
        [SwaggerResponse(401, "User not authenticated")]
        [ProducesResponseType(typeof(ApiResponse<WalletResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMyWallet()
        {
            var userId = GetCurrentUserId();
            var wallet = await _walletService.GetWalletByUserIdAsync(userId);

            return Ok(ApiResponseBuilder.BuildSuccessResponse(wallet, "Wallet retrieved successfully"));
        }

        /// <summary>
        /// Create wallet payment for snake catching service
        /// </summary>
        /// <param name="request">Payment request with catching request ID and amount</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Payment confirmation response</returns>
        [HttpPost("payment")]
        [Authorize]
        [SwaggerOperation(
            Summary = "Create Wallet Payment",
            Description = "Process payment using user's wallet balance for a snake catching request. Transfers money from user's wallet to system account and confirms payment immediately.",
            Tags = new[] { "Wallet", "Payments" })]
        [ProducesResponseType(typeof(SnakeCatchingPaymentResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CreateWalletPayment(
            [FromBody] CreateSnakeCatchingPaymentRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var result = await _walletPaymentService.CreateWalletPaymentAsync(request, currentUserId, cancellationToken);
                return Ok(new
                {
                    success = true,
                    message = "Wallet payment processed successfully",
                    data = result
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation when creating wallet payment");
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating wallet payment");
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while processing wallet payment"
                });
            }
        }
    }
}
