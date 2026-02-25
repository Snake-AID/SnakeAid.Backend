using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Responses.Transaction;
using SnakeAid.Service.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SnakeAid.Api.Controllers
{
    [Route("api/transactions")]
    [ApiController]
    [Authorize]
    public class TransactionController : BaseController<TransactionController>
    {
        private readonly ITransactionService _transactionService;

        public TransactionController(
            ILogger<TransactionController> logger,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            ITransactionService transactionService)
            : base(logger, httpContextAccessor, mapper)
        {
            _transactionService = transactionService;
        }

        /// <summary>
        /// Get transaction details by snake catching request ID
        /// </summary>
        /// <param name="snakeCatchingRequestId">The ID of the snake catching request</param>
        [HttpGet("snakecatchingrequest/{snakeCatchingRequestId:guid}")]
        [SwaggerOperation(
            Summary = "Get Transaction by Snake Catching Request ID",
            Description = "Retrieve transaction details associated with a specific snake catching request (CatchingPayment, CatchingDeposit, CatchingRefund, or CatcherPayout)")]
        [SwaggerResponse(200, "Transaction retrieved successfully", typeof(ApiResponse<TransactionResponse>))]
        [SwaggerResponse(404, "Transaction not found for the given snake catching request")]
        [SwaggerResponse(401, "User not authenticated")]
        [ProducesResponseType(typeof(ApiResponse<TransactionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTransactionBySnakeCatchingRequestId([FromRoute] Guid snakeCatchingRequestId)
        {
            var transaction = await _transactionService.GetTransactionBySnakeCatchingRequestIdAsync(snakeCatchingRequestId);

            if (transaction == null)
            {
                return NotFound(ApiResponseBuilder.BuildErrorResponse(
                    $"No transaction found for snake catching request ID: {snakeCatchingRequestId}"));
            }

            return Ok(ApiResponseBuilder.BuildSuccessResponse(
                transaction,
                "Transaction retrieved successfully."));
        }
    }
}
