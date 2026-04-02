using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
        public async Task<IActionResult> GetAllWithdrawals()
        {
            var withdrawals = await _walletWithdrawService.GetAllWithdrawalsAsync();
            var responses = withdrawals.Select(w => w.Adapt<WithdrawalResponse>());
            return Ok(responses);
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingWithdrawals()
        {
            var withdrawals = await _walletWithdrawService.GetPendingWithdrawalsAsync();
            var responses = withdrawals.Select(w => w.Adapt<WithdrawalResponse>());
            return Ok(responses);
        }

        [HttpPost("{id}/approve")]
        public async Task<IActionResult> ApproveWithdrawal(Guid id)
        {
            var adminUserId = GetCurrentUserId().ToString();
            var withdrawal = await _walletWithdrawService.ApproveWithdrawalAsync(id, adminUserId);

            var response = withdrawal.Adapt<WithdrawalResponse>();
            return Ok(response);
        }

        [HttpPost("{id}/reject")]
        public async Task<IActionResult> RejectWithdrawal(Guid id, [FromBody] RejectWithdrawalRequest request)
        {
            var adminUserId = GetCurrentUserId().ToString();
            var withdrawal = await _walletWithdrawService.RejectWithdrawalAsync(id, adminUserId, request.Reason);

            var response = withdrawal.Adapt<WithdrawalResponse>();
            return Ok(response);
        }

        [HttpPost("{id}/complete")]
        public async Task<IActionResult> CompleteWithdrawal(Guid id)
        {
            var adminUserId = GetCurrentUserId().ToString();
            var withdrawal = await _walletWithdrawService.CompleteWithdrawalAsync(id, adminUserId);

            var response = withdrawal.Adapt<WithdrawalResponse>();
            return Ok(response);
        }

        [HttpPost("{id}/fail")]
        public async Task<IActionResult> FailWithdrawal(Guid id, [FromBody] FailWithdrawalRequest request)
        {
            var adminUserId = GetCurrentUserId().ToString();
            var withdrawal = await _walletWithdrawService.FailWithdrawalAsync(id, adminUserId, request.Reason);

            var response = withdrawal.Adapt<WithdrawalResponse>();
            return Ok(response);
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