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
        private const decimal MinWithdrawalAmount = 50000m;
        private const decimal MaxWithdrawalAmount = 5000000m;
        private const decimal DailyWithdrawalLimit = 10000000m;

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

        public async Task<WalletWithdraw> CreateWithdrawalRequestAsync(Guid userId, decimal amount, string bankAccount, string bankName, string accountHolderName, string bankBin)
        {
            ValidateWithdrawalAmount(amount);

            // Validate user wallet
            var wallet = await _walletService.GetWalletByUserIdAsync(userId);
            var pendingAmount = await GetPendingWithdrawalAmountAsync(userId);
            var availableBalance = wallet.Balance - pendingAmount;
            if (availableBalance < amount)
            {
                throw new InvalidOperationException("Insufficient wallet balance");
            }

            var todayTotal = await GetTodayWithdrawalAmountAsync(userId);
            if (todayTotal + amount > DailyWithdrawalLimit)
            {
                throw new InvalidOperationException($"Daily withdrawal limit is {DailyWithdrawalLimit:N0} VND");
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
                AccountHolderName = accountHolderName,
                BankBin = bankBin,
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

        public async Task<WalletWithdraw> ApproveWithdrawalAsync(Guid withdrawalId, Guid adminUserId, string? adminNotes)
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

            if (string.IsNullOrWhiteSpace(withdrawal.BankBin))
            {
                throw new InvalidOperationException("Withdrawal bank BIN is missing and QR cannot be generated");
            }

            if (withdrawal.Wallet.Balance < withdrawal.Amount)
            {
                throw new InvalidOperationException("Insufficient wallet balance to approve withdrawal");
            }

            // Generate QR code
            var (payload, imageBase64) = _vietQrAdapter.GenerateQr(
                withdrawal.BankBin,
                withdrawal.BankAccount,
                withdrawal.AccountHolderName,
                withdrawal.Amount,
                $"Withdrawal {withdrawal.Id}");

            withdrawal.Wallet.Balance -= withdrawal.Amount;
            withdrawal.Status = WalletWithdrawStatus.Approved;
            withdrawal.ProcessedByAdminId = adminUserId;
            withdrawal.AdminNotes = NormalizeText(adminNotes);
            withdrawal.VietQrPayload = payload;
            withdrawal.VietQrImageBase64 = imageBase64;
            withdrawal.ProcessedAt = DateTime.UtcNow;

            await _unitOfWork.GetRepository<Transaction>().InsertAsync(CreateWithdrawalTransaction(withdrawal));
            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Approved withdrawal {WithdrawalId} by admin {AdminUserId}", withdrawalId, adminUserId);

            return withdrawal;
        }

        public async Task<WalletWithdraw> RejectWithdrawalAsync(Guid withdrawalId, Guid adminUserId, string reason, string? adminNotes)
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

            if (withdrawal.Status != WalletWithdrawStatus.Pending && withdrawal.Status != WalletWithdrawStatus.Approved)
            {
                throw new InvalidOperationException("Can only reject pending or approved withdrawals");
            }

            if (withdrawal.Status == WalletWithdrawStatus.Approved)
            {
                withdrawal.Wallet.Balance += withdrawal.Amount;
                await _unitOfWork.GetRepository<Transaction>().InsertAsync(
                    CreateWithdrawalRefundTransaction(withdrawal, "Withdrawal rejected after approval"));
            }

            withdrawal.Status = WalletWithdrawStatus.Rejected;
            withdrawal.ProcessedByAdminId = adminUserId;
            withdrawal.RejectionReason = reason;
            withdrawal.AdminNotes = NormalizeText(adminNotes);
            withdrawal.VietQrPayload = null;
            withdrawal.VietQrImageBase64 = null;
            withdrawal.ProcessedAt = DateTime.UtcNow;

            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Rejected withdrawal {WithdrawalId} by admin {AdminUserId}: {Reason}", withdrawalId, adminUserId, reason);

            return withdrawal;
        }

        public async Task<WalletWithdraw> CompleteWithdrawalAsync(Guid withdrawalId, Guid adminUserId, string? adminNotes)
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

            withdrawal.Status = WalletWithdrawStatus.Completed;
            withdrawal.ProcessedByAdminId = adminUserId;
            withdrawal.AdminNotes = NormalizeText(adminNotes) ?? withdrawal.AdminNotes;
            withdrawal.ProcessedAt = DateTime.UtcNow;

            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Completed withdrawal {WithdrawalId} by admin {AdminUserId}", withdrawalId, adminUserId);

            return withdrawal;
        }

        public async Task<WalletWithdraw> FailWithdrawalAsync(Guid withdrawalId, Guid adminUserId, string reason, string? adminNotes)
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
                throw new InvalidOperationException("Can only fail approved withdrawals");
            }

            withdrawal.Wallet.Balance += withdrawal.Amount;
            withdrawal.Status = WalletWithdrawStatus.Failed;
            withdrawal.ProcessedByAdminId = adminUserId;
            withdrawal.RejectionReason = reason;
            withdrawal.AdminNotes = NormalizeText(adminNotes);
            withdrawal.VietQrPayload = null;
            withdrawal.VietQrImageBase64 = null;
            withdrawal.ProcessedAt = DateTime.UtcNow;

            await _unitOfWork.GetRepository<Transaction>().InsertAsync(
                CreateWithdrawalRefundTransaction(withdrawal, "Withdrawal failed after approval"));
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

        private static void ValidateWithdrawalAmount(decimal amount)
        {
            if (amount < MinWithdrawalAmount || amount > MaxWithdrawalAmount)
            {
                throw new InvalidOperationException(
                    $"Withdrawal amount must be between {MinWithdrawalAmount:N0} and {MaxWithdrawalAmount:N0} VND");
            }
        }

        private async Task<decimal> GetPendingWithdrawalAmountAsync(Guid userId)
        {
            var pendingWithdrawals = await _unitOfWork.GetRepository<WalletWithdraw>()
                .GetListAsync(predicate: w => w.UserId == userId && w.Status == WalletWithdrawStatus.Pending);

            return pendingWithdrawals.Sum(w => w.Amount);
        }

        private async Task<decimal> GetTodayWithdrawalAmountAsync(Guid userId)
        {
            var startOfTodayUtc = DateTime.UtcNow.Date;
            var endOfTodayUtc = startOfTodayUtc.AddDays(1);

            var withdrawals = await _unitOfWork.GetRepository<WalletWithdraw>()
                .GetListAsync(predicate: w =>
                    w.UserId == userId &&
                    w.CreatedAt >= startOfTodayUtc &&
                    w.CreatedAt < endOfTodayUtc &&
                    w.Status != WalletWithdrawStatus.Rejected &&
                    w.Status != WalletWithdrawStatus.Failed);

            return withdrawals.Sum(w => w.Amount);
        }

        private static string? NormalizeText(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static Transaction CreateWithdrawalTransaction(WalletWithdraw withdrawal)
        {
            return new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = withdrawal.UserId,
                ReferenceId = withdrawal.Id,
                Amount = -withdrawal.Amount,
                TransactionType = TransactionType.WalletWithdraw,
                Description = $"Wallet withdrawal approved to {withdrawal.BankName} - {withdrawal.BankAccount}",
                CreatedAt = DateTime.UtcNow
            };
        }

        private static Transaction CreateWithdrawalRefundTransaction(WalletWithdraw withdrawal, string description)
        {
            return new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = withdrawal.UserId,
                ReferenceId = withdrawal.Id,
                Amount = withdrawal.Amount,
                TransactionType = TransactionType.AdminAdjustment,
                Description = description,
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}
