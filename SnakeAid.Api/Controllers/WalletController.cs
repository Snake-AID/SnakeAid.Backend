using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Meta;
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

        public WalletController(
            ILogger<WalletController> logger,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            IWalletService walletService)
            : base(logger, httpContextAccessor, mapper)
        {
            _walletService = walletService;
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
    }
}
