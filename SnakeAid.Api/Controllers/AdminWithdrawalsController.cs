using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Responses.Wallet;
using SnakeAid.Service.Interfaces;
using Microsoft.AspNetCore.Http;
using MapsterMapper;

namespace SnakeAid.Api.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/admin/withdrawals")]
    public class AdminWithdrawalsController : BaseController<AdminWithdrawalsController>
    {
        private readonly IWalletWithdrawService _walletWithdrawService;

        public AdminWithdrawalsController(
            IWalletWithdrawService walletWithdrawService,
            ILogger<AdminWithdrawalsController> logger,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper)
            : base(logger, httpContextAccessor, mapper)
        {
            _walletWithdrawService = walletWithdrawService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<WithdrawalResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllWithdrawals()
        {
            var withdrawals = await _walletWithdrawService.GetAllWithdrawalsAsync();
            var responses = withdrawals.Select(w => w.Adapt<WithdrawalResponse>());
            return Ok(ApiResponseBuilder.BuildSuccessResponse(responses));
        }

        [HttpGet("pending")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<WithdrawalResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPendingWithdrawals()
        {
            var withdrawals = await _walletWithdrawService.GetPendingWithdrawalsAsync();
            var responses = withdrawals.Select(w => w.Adapt<WithdrawalResponse>());
            return Ok(ApiResponseBuilder.BuildSuccessResponse(responses));
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<WithdrawalResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetWithdrawalById(Guid id)
        {
            var withdrawal = await _walletWithdrawService.GetWithdrawalByIdAsync(id);
            if (withdrawal == null)
            {
                return NotFound(ApiResponseBuilder.BuildNotFoundResponse("Withdrawal not found"));
            }

            var response = withdrawal.Adapt<WithdrawalResponse>();
            return Ok(ApiResponseBuilder.BuildSuccessResponse(response));
        }

        [HttpPost("{id}/approve")]
        [ProducesResponseType(typeof(ApiResponse<WithdrawalResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ApproveWithdrawal(Guid id)
        {
            var adminUserId = GetCurrentUserId().ToString();
            var withdrawal = await _walletWithdrawService.ApproveWithdrawalAsync(id, adminUserId);

            var response = withdrawal.Adapt<WithdrawalResponse>();
            return Ok(ApiResponseBuilder.BuildSuccessResponse(response));
        }

        [HttpPost("{id}/reject")]
        [ProducesResponseType(typeof(ApiResponse<WithdrawalResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> RejectWithdrawal(Guid id, [FromBody] RejectWithdrawalRequest request)
        {
            var adminUserId = GetCurrentUserId().ToString();
            var withdrawal = await _walletWithdrawService.RejectWithdrawalAsync(id, adminUserId, request.Reason);

            var response = withdrawal.Adapt<WithdrawalResponse>();
            return Ok(ApiResponseBuilder.BuildSuccessResponse(response));
        }

        [HttpPost("{id}/complete")]
        [ProducesResponseType(typeof(ApiResponse<WithdrawalResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CompleteWithdrawal(Guid id)
        {
            var adminUserId = GetCurrentUserId().ToString();
            var withdrawal = await _walletWithdrawService.CompleteWithdrawalAsync(id, adminUserId);

            var response = withdrawal.Adapt<WithdrawalResponse>();
            return Ok(ApiResponseBuilder.BuildSuccessResponse(response));
        }

        [HttpPost("{id}/fail")]
        [ProducesResponseType(typeof(ApiResponse<WithdrawalResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> FailWithdrawal(Guid id, [FromBody] FailWithdrawalRequest request)
        {
            var adminUserId = GetCurrentUserId().ToString();
            var withdrawal = await _walletWithdrawService.FailWithdrawalAsync(id, adminUserId, request.Reason);

            var response = withdrawal.Adapt<WithdrawalResponse>();
            return Ok(ApiResponseBuilder.BuildSuccessResponse(response));
        }
    }

    public class RejectWithdrawalRequest
    {
        public string Reason { get; set; }
    }

    public class FailWithdrawalRequest
    {
        public string Reason { get; set; }
    }
}
