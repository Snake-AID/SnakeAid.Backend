using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.Transaction;
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
        /// Get transactions by user ID and transaction group type
        /// </summary>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Get Transactions List",
            Description = "Get paged transactions with optional filters. If userId and transType are both provided, they are applied with AND condition. If both are null, all transactions are returned. Supported transType values: consultation, snake catching, snakebite incident, system.")]
        [SwaggerResponse(200, "Transactions retrieved successfully", typeof(ApiResponse<PagedData<TransactionResponse>>))]
        [SwaggerResponse(400, "Invalid query parameters")]
        [ProducesResponseType(typeof(ApiResponse<PagedData<TransactionResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetTransactions([FromQuery] GetTransactionsRequest request, CancellationToken ct)
        {
            var transactions = await _transactionService.GetTransactionsAsync(request, ct);

            return Ok(ApiResponseBuilder.BuildSuccessResponse(
                transactions,
                "Transactions retrieved successfully."));
        }

        /// <summary>
        /// Get transaction detail by transaction ID
        /// </summary>
        [HttpGet("{id:guid}")]
        [SwaggerOperation(
            Summary = "Get Transaction Detail",
            Description = "Retrieve transaction detail by transaction ID")]
        [SwaggerResponse(200, "Transaction retrieved successfully", typeof(ApiResponse<TransactionResponse>))]
        [SwaggerResponse(404, "Transaction not found")]
        [ProducesResponseType(typeof(ApiResponse<TransactionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTransactionDetail([FromRoute] Guid id, CancellationToken ct)
        {
            var transaction = await _transactionService.GetTransactionDetailAsync(id, ct);

            if (transaction == null)
            {
                return NotFound(ApiResponseBuilder.BuildErrorResponse(
                    $"Transaction with ID {id} was not found."));
            }

            return Ok(ApiResponseBuilder.BuildSuccessResponse(
                transaction,
                "Transaction retrieved successfully."));
        }

    }
}
