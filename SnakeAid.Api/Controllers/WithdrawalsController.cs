using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Requests.Wallet;
using SnakeAid.Core.Responses.Wallet;
using SnakeAid.Service.Interfaces;
using Microsoft.AspNetCore.Http;
using MapsterMapper;

namespace SnakeAid.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/withdrawals")]
    public class WithdrawalsController : BaseController<WithdrawalsController>
    {
        private readonly IWalletWithdrawService _walletWithdrawService;

        public WithdrawalsController(
            IWalletWithdrawService walletWithdrawService,
            ILogger<WithdrawalsController> logger,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper)
            : base(logger, httpContextAccessor, mapper)
        {
            _walletWithdrawService = walletWithdrawService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateWithdrawal([FromBody] CreateWithdrawalRequest request)
        {
            var userId = GetCurrentUserId();
            var withdrawal = await _walletWithdrawService.CreateWithdrawalRequestAsync(
                userId, request.Amount, request.BankAccount, request.BankName, request.BankBin);

            var response = withdrawal.Adapt<WithdrawalResponse>();
            // Mask bank account for security
            response.BankAccount = MaskBankAccount(response.BankAccount);

            return Ok(response);
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyWithdrawals()
        {
            var userId = GetCurrentUserId();
            var withdrawals = await _walletWithdrawService.GetUserWithdrawalsAsync(userId);

            var responses = withdrawals.Select(w =>
            {
                var response = w.Adapt<WithdrawalResponse>();
                response.BankAccount = MaskBankAccount(response.BankAccount);
                return response;
            });

            return Ok(responses);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetWithdrawalById(Guid id)
        {
            var withdrawal = await _walletWithdrawService.GetWithdrawalByIdAsync(id);
            if (withdrawal == null)
            {
                return NotFound();
            }

            // Check if user owns this withdrawal
            var userId = GetCurrentUserId();
            if (withdrawal.UserId != userId)
            {
                return Forbid();
            }

            var response = withdrawal.Adapt<WithdrawalResponse>();
            response.BankAccount = MaskBankAccount(response.BankAccount);

            return Ok(response);
        }

        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelWithdrawal(Guid id)
        {
            var userId = GetCurrentUserId();
            var withdrawal = await _walletWithdrawService.CancelWithdrawalAsync(id, userId);

            var response = withdrawal.Adapt<WithdrawalResponse>();
            response.BankAccount = MaskBankAccount(response.BankAccount);

            return Ok(response);
        }

        private string MaskBankAccount(string account)
        {
            if (string.IsNullOrEmpty(account) || account.Length <= 4)
            {
                return account;
            }

            var visibleChars = 4;
            var masked = new string('*', account.Length - visibleChars) + account.Substring(account.Length - visibleChars);
            return masked;
        }
    }
}