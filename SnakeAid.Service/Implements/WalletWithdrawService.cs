using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Domains;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Service.Implements
{
    public class WalletWithdrawService : IWalletWithdrawService
    {
        private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
        private readonly IWalletService _walletService;
        private readonly VietQrAdapter _vietQrAdapter;
        private readonly ILogger<WalletWithdrawService> _logger;

        public WalletWithdrawService(
            IUnitOfWork<SnakeAidDbContext> unitOfWork,
            IWalletService walletService,
            VietQrAdapter vietQrAdapter,
            ILogger<WalletWithdrawService> logger)
        {
            _unitOfWork = unitOfWork;
            _walletService = walletService;
            _vietQrAdapter = vietQrAdapter;
            _logger = logger;
        }

        public async Task<WalletWithdraw> CreateWithdrawalRequestAsync(Guid userId, decimal amount, string bankAccount, string bankName, string bankBin)
        {
            // Validate user wallet
            var wallet = await _walletService.GetWalletByUserIdAsync(userId);
            if (wallet.Balance < amount)
            {
                throw new InvalidOperationException("Insufficient wallet balance");
            }

            // Create withdrawal request
            var withdrawal = new WalletWithdraw
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                WalletId = wallet.Id,
                Amount = amount,
                BankAccount = bankAccount,
                BankName = bankName,
                Status = WalletWithdrawStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            // Generate QR code for approved withdrawals
            // Note: QR is generated when approved, not when created

            await _unitOfWork.GetRepository<WalletWithdraw>().InsertAsync(withdrawal);
            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Created withdrawal request {WithdrawalId} for user {UserId}", withdrawal.Id, userId);

            return withdrawal;
        }

        public async Task<WalletWithdraw> GetWithdrawalByIdAsync(Guid withdrawalId)
        {
            return await _unitOfWork.GetRepository<WalletWithdraw>()
                .GetByIdAsync(withdrawalId);
        }

        public async Task<IEnumerable<WalletWithdraw>> GetUserWithdrawalsAsync(Guid userId)
        {
            return await _unitOfWork.GetRepository<WalletWithdraw>()
                .GetListAsync(predicate: w => w.UserId == userId, orderBy: q => q.OrderByDescending(w => w.CreatedAt));
        }

        public async Task<WalletWithdraw> CancelWithdrawalAsync(Guid withdrawalId, Guid userId)
        {
            var withdrawal = await GetWithdrawalByIdAsync(withdrawalId);
            if (withdrawal == null || withdrawal.UserId != userId)
            {
                throw new InvalidOperationException("Withdrawal not found or access denied");
            }

            if (withdrawal.Status != WalletWithdrawStatus.Pending)
            {
                throw new InvalidOperationException("Can only cancel pending withdrawals");
            }

            withdrawal.Status = WalletWithdrawStatus.Rejected;
            withdrawal.RejectionReason = "Cancelled by user";
            withdrawal.ProcessedAt = DateTime.UtcNow;

            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Cancelled withdrawal {WithdrawalId} by user {UserId}", withdrawalId, userId);

            return withdrawal;
        }

        public async Task<WalletWithdraw> ApproveWithdrawalAsync(Guid withdrawalId, string adminUserId)
        {
            var withdrawal = await _unitOfWork.GetRepository<WalletWithdraw>()
                .FirstOrDefaultAsync(
                    predicate: w => w.Id == withdrawalId,
                    include: q => q.Include(w => w.Wallet)
                );

            if (withdrawal == null)
            {
                throw new InvalidOperationException("Withdrawal not found");
            }

            if (withdrawal.Status != WalletWithdrawStatus.Pending)
            {
                throw new InvalidOperationException("Can only approve pending withdrawals");
            }

            // Generate QR code
            var (payload, imageBase64) = _vietQrAdapter.GenerateQr(
                "970400", // Default bank bin, should be from bank directory
                withdrawal.BankAccount,
                withdrawal.BankName,
                withdrawal.Amount,
                $"Withdrawal {withdrawal.Id}");

            withdrawal.Status = WalletWithdrawStatus.Approved;
            withdrawal.VietQrPayload = payload;
            withdrawal.VietQrImageBase64 = imageBase64;
            withdrawal.ProcessedAt = DateTime.UtcNow;

            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Approved withdrawal {WithdrawalId} by admin {AdminUserId}", withdrawalId, adminUserId);

            return withdrawal;
        }

        public async Task<WalletWithdraw> RejectWithdrawalAsync(Guid withdrawalId, string adminUserId, string reason)
        {
            var withdrawal = await GetWithdrawalByIdAsync(withdrawalId);
            if (withdrawal == null)
            {
                throw new InvalidOperationException("Withdrawal not found");
            }

            if (withdrawal.Status != WalletWithdrawStatus.Pending && withdrawal.Status != WalletWithdrawStatus.Approved)
            {
                throw new InvalidOperationException("Can only reject pending or approved withdrawals");
            }

            withdrawal.Status = WalletWithdrawStatus.Rejected;
            withdrawal.RejectionReason = reason;
            withdrawal.ProcessedAt = DateTime.UtcNow;

            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Rejected withdrawal {WithdrawalId} by admin {AdminUserId}: {Reason}", withdrawalId, adminUserId, reason);

            return withdrawal;
        }

        public async Task<WalletWithdraw> CompleteWithdrawalAsync(Guid withdrawalId, string adminUserId)
        {
            var withdrawal = await _unitOfWork.GetRepository<WalletWithdraw>()
                .FirstOrDefaultAsync(
                    predicate: w => w.Id == withdrawalId,
                    include: q => q.Include(w => w.Wallet)
                );

            if (withdrawal == null)
            {
                throw new InvalidOperationException("Withdrawal not found");
            }

            if (withdrawal.Status != WalletWithdrawStatus.Approved)
            {
                throw new InvalidOperationException("Can only complete approved withdrawals");
            }

            // Deduct from wallet balance
            withdrawal.Wallet.Balance -= withdrawal.Amount;

            // Create transaction record
            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = withdrawal.UserId,
                ReferenceId = withdrawal.Id,
                Amount = -withdrawal.Amount,
                TransactionType = TransactionType.WalletWithdraw,
                Description = $"Wallet withdrawal to {withdrawal.BankName} - {withdrawal.BankAccount}",
                CreatedAt = DateTime.UtcNow
            };

            withdrawal.Status = WalletWithdrawStatus.Completed;
            withdrawal.ProcessedAt = DateTime.UtcNow;

            await _unitOfWork.GetRepository<Transaction>().InsertAsync(transaction);
            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Completed withdrawal {WithdrawalId} by admin {AdminUserId}", withdrawalId, adminUserId);

            return withdrawal;
        }

        public async Task<WalletWithdraw> FailWithdrawalAsync(Guid withdrawalId, string adminUserId, string reason)
        {
            var withdrawal = await GetWithdrawalByIdAsync(withdrawalId);
            if (withdrawal == null)
            {
                throw new InvalidOperationException("Withdrawal not found");
            }

            if (withdrawal.Status != WalletWithdrawStatus.Approved)
            {
                throw new InvalidOperationException("Can only fail approved withdrawals");
            }

            withdrawal.Status = WalletWithdrawStatus.Failed;
            withdrawal.RejectionReason = reason;
            withdrawal.ProcessedAt = DateTime.UtcNow;

            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Failed withdrawal {WithdrawalId} by admin {AdminUserId}: {Reason}", withdrawalId, adminUserId, reason);

            return withdrawal;
        }

        public async Task<IEnumerable<WalletWithdraw>> GetPendingWithdrawalsAsync()
        {
            return await _unitOfWork.GetRepository<WalletWithdraw>()
                .GetListAsync(
                    predicate: w => w.Status == WalletWithdrawStatus.Pending,
                    orderBy: q => q.OrderBy(w => w.CreatedAt)
                );
        }

        public async Task<IEnumerable<WalletWithdraw>> GetAllWithdrawalsAsync()
        {
            return await _unitOfWork.GetRepository<WalletWithdraw>()
                .GetListAsync(orderBy: q => q.OrderByDescending(w => w.CreatedAt));
        }
    }
}