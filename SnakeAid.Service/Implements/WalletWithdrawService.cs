using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Constants;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Messages.Notifications;
using SnakeAid.Core.Requests.Notification;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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
        private readonly INotificationQueueService _notificationQueueService;
        private readonly ILogger<WalletWithdrawService> _logger;

        public WalletWithdrawService(
            IUnitOfWork<SnakeAidDbContext> unitOfWork,
            IWalletService walletService,
            VietQrAdapter vietQrAdapter,
            INotificationQueueService notificationQueueService,
            ILogger<WalletWithdrawService> logger)
        {
            _unitOfWork = unitOfWork;
            _walletService = walletService;
            _vietQrAdapter = vietQrAdapter;
            _notificationQueueService = notificationQueueService;
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
                throw new ConflictException(
                    "Insufficient wallet balance",
                    WithdrawalErrorCodes.WithdrawalInsufficientBalance);
            }

            var todayTotal = await GetTodayWithdrawalAmountAsync(userId);
            if (todayTotal + amount > DailyWithdrawalLimit)
            {
                throw new ConflictException(
                    $"Daily withdrawal limit is {DailyWithdrawalLimit:N0} VND",
                    WithdrawalErrorCodes.WithdrawalDailyLimitExceeded);
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

            await _notificationQueueService.BroadcastAsync(new AdminBroadcastNotificationRequest
            {
                Title = "New withdrawal request",
                Body = $"A user submitted a withdrawal request of {withdrawal.Amount:N0} VND.",
                Type = "WITHDRAWAL_REQUEST_CREATED",
                TargetRoles = new List<AccountRole> { AccountRole.Admin },
                Data = BuildNotificationData(withdrawal.Id, withdrawal.Status, withdrawal.UserId)
            });

            _logger.LogInformation("Created withdrawal request {WithdrawalId} for user {UserId}", withdrawal.Id, userId);

            return withdrawal;
        }

        public async Task<WalletWithdraw> GetWithdrawalByIdAsync(Guid withdrawalId)
        {
            return await _unitOfWork.GetRepository<WalletWithdraw>()
                .GetByIdAsync(withdrawalId) ?? null!;
        }

        public async Task<IEnumerable<WalletWithdraw>> GetUserWithdrawalsAsync(Guid userId)
        {
            return await _unitOfWork.GetRepository<WalletWithdraw>()
                .GetListAsync(predicate: w => w.UserId == userId, orderBy: q => q.OrderByDescending(w => w.CreatedAt));
        }

        public async Task<WalletWithdraw> CancelWithdrawalAsync(Guid withdrawalId, Guid userId)
        {
            var withdrawal = await GetWithdrawalByIdAsync(withdrawalId);
            if (withdrawal == null)
            {
                throw new NotFoundException(
                    "Withdrawal not found",
                    WithdrawalErrorCodes.WithdrawalNotFound);
            }

            if (withdrawal.UserId != userId)
            {
                throw new ForbiddenException(
                    "You are not allowed to access this withdrawal",
                    WithdrawalErrorCodes.WithdrawalForbidden);
            }

            if (withdrawal.Status != WalletWithdrawStatus.Pending)
            {
                throw new ConflictException(
                    "Can only cancel pending withdrawals",
                    WithdrawalErrorCodes.WithdrawalInvalidStatus);
            }

            withdrawal.Status = WalletWithdrawStatus.Rejected;
            withdrawal.RejectionReason = "Cancelled by user";
            withdrawal.ProcessedAt = DateTime.UtcNow;

            await _unitOfWork.CommitAsync();

            await _notificationQueueService.BroadcastAsync(new AdminBroadcastNotificationRequest
            {
                Title = "Withdrawal cancelled",
                Body = $"User cancelled withdrawal request {withdrawal.Id}.",
                Type = "WITHDRAWAL_CANCELLED",
                TargetRoles = new List<AccountRole> { AccountRole.Admin },
                Data = BuildNotificationData(withdrawal.Id, withdrawal.Status, withdrawal.UserId)
            });

            _logger.LogInformation("Cancelled withdrawal {WithdrawalId} by user {UserId}", withdrawalId, userId);

            return withdrawal;
        }

        public async Task<WalletWithdraw> ApproveWithdrawalAsync(Guid withdrawalId, Guid adminUserId, string? adminNotes)
        {
            var withdrawal = await _unitOfWork.GetRepository<WalletWithdraw>()
                .FirstOrDefaultAsync(
                    predicate: w => w.Id == withdrawalId,
                    include: q => q.Include(w => w.Wallet),
                    asNoTracking: false
                );

            if (withdrawal == null)
            {
                throw new NotFoundException(
                    "Withdrawal not found",
                    WithdrawalErrorCodes.WithdrawalNotFound);
            }

            if (withdrawal.Status != WalletWithdrawStatus.Pending)
            {
                throw new ConflictException(
                    "Can only approve pending withdrawals",
                    WithdrawalErrorCodes.WithdrawalInvalidStatus);
            }

            if (string.IsNullOrWhiteSpace(withdrawal.BankBin))
            {
                throw new ConflictException(
                    "Withdrawal bank BIN is missing and QR cannot be generated",
                    WithdrawalErrorCodes.WithdrawalBankBinMissing);
            }

            if (withdrawal.Wallet.Balance < withdrawal.Amount)
            {
                throw new ConflictException(
                    "Insufficient wallet balance to approve withdrawal",
                    WithdrawalErrorCodes.WithdrawalInsufficientBalance);
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

            await PublishUserNotificationAsync(
                withdrawal.UserId,
                "Withdrawal approved",
                $"Your withdrawal request of {withdrawal.Amount:N0} VND has been approved.",
                "WITHDRAWAL_APPROVED",
                BuildNotificationData(withdrawal.Id, withdrawal.Status, withdrawal.UserId));

            _logger.LogInformation("Approved withdrawal {WithdrawalId} by admin {AdminUserId}", withdrawalId, adminUserId);

            return withdrawal;
        }

        public async Task<WalletWithdraw> RejectWithdrawalAsync(Guid withdrawalId, Guid adminUserId, string reason, string? adminNotes)
        {
            var withdrawal = await _unitOfWork.GetRepository<WalletWithdraw>()
                .FirstOrDefaultAsync(
                    predicate: w => w.Id == withdrawalId,
                    include: q => q.Include(w => w.Wallet),
                    asNoTracking: false
                );
            if (withdrawal == null)
            {
                throw new NotFoundException(
                    "Withdrawal not found",
                    WithdrawalErrorCodes.WithdrawalNotFound);
            }

            if (withdrawal.Status != WalletWithdrawStatus.Pending && withdrawal.Status != WalletWithdrawStatus.Approved)
            {
                throw new ConflictException(
                    "Can only reject pending or approved withdrawals",
                    WithdrawalErrorCodes.WithdrawalInvalidStatus);
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

            await PublishUserNotificationAsync(
                withdrawal.UserId,
                "Withdrawal rejected",
                $"Your withdrawal request of {withdrawal.Amount:N0} VND was rejected.",
                "WITHDRAWAL_REJECTED",
                BuildNotificationData(withdrawal.Id, withdrawal.Status, withdrawal.UserId, reason));

            _logger.LogInformation("Rejected withdrawal {WithdrawalId} by admin {AdminUserId}: {Reason}", withdrawalId, adminUserId, reason);

            return withdrawal;
        }

        public async Task<WalletWithdraw> CompleteWithdrawalAsync(Guid withdrawalId, Guid adminUserId, string? adminNotes)
        {
            var withdrawal = await _unitOfWork.GetRepository<WalletWithdraw>()
                .FirstOrDefaultAsync(
                    predicate: w => w.Id == withdrawalId,
                    include: q => q.Include(w => w.Wallet),
                    asNoTracking: false
                );

            if (withdrawal == null)
            {
                throw new NotFoundException(
                    "Withdrawal not found",
                    WithdrawalErrorCodes.WithdrawalNotFound);
            }

            if (withdrawal.Status != WalletWithdrawStatus.Approved)
            {
                throw new ConflictException(
                    "Can only complete approved withdrawals",
                    WithdrawalErrorCodes.WithdrawalInvalidStatus);
            }

            withdrawal.Status = WalletWithdrawStatus.Completed;
            withdrawal.ProcessedByAdminId = adminUserId;
            withdrawal.AdminNotes = NormalizeText(adminNotes) ?? withdrawal.AdminNotes;
            withdrawal.ProcessedAt = DateTime.UtcNow;

            await _unitOfWork.CommitAsync();

            await PublishUserNotificationAsync(
                withdrawal.UserId,
                "Withdrawal completed",
                $"Your withdrawal request of {withdrawal.Amount:N0} VND has been completed.",
                "WITHDRAWAL_COMPLETED",
                BuildNotificationData(withdrawal.Id, withdrawal.Status, withdrawal.UserId));

            _logger.LogInformation("Completed withdrawal {WithdrawalId} by admin {AdminUserId}", withdrawalId, adminUserId);

            return withdrawal;
        }

        public async Task<WalletWithdraw> FailWithdrawalAsync(Guid withdrawalId, Guid adminUserId, string reason, string? adminNotes)
        {
            var withdrawal = await _unitOfWork.GetRepository<WalletWithdraw>()
                .FirstOrDefaultAsync(
                    predicate: w => w.Id == withdrawalId,
                    include: q => q.Include(w => w.Wallet),
                    asNoTracking: false
                );
            if (withdrawal == null)
            {
                throw new NotFoundException(
                    "Withdrawal not found",
                    WithdrawalErrorCodes.WithdrawalNotFound);
            }

            if (withdrawal.Status != WalletWithdrawStatus.Approved)
            {
                throw new ConflictException(
                    "Can only fail approved withdrawals",
                    WithdrawalErrorCodes.WithdrawalInvalidStatus);
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

            await PublishUserNotificationAsync(
                withdrawal.UserId,
                "Withdrawal failed",
                $"Your withdrawal request of {withdrawal.Amount:N0} VND failed during processing.",
                "WITHDRAWAL_FAILED",
                BuildNotificationData(withdrawal.Id, withdrawal.Status, withdrawal.UserId, reason));

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
                throw new ValidationException(
                    "Validation failed",
                    new Dictionary<string, string[]>
                    {
                        ["amount"] =
                        [
                            $"The field Amount must be between {MinWithdrawalAmount:N0} and {MaxWithdrawalAmount:N0}."
                        ]
                    },
                    "VALIDATION_ERROR");
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

        private async Task PublishUserNotificationAsync(
            Guid userId,
            string title,
            string body,
            string type,
            Dictionary<string, string> data)
        {
            await _notificationQueueService.PublishAsync(new NotificationMessage
            {
                UserId = userId,
                Title = title,
                Body = body,
                Type = type,
                Data = data
            });
        }

        private static Dictionary<string, string> BuildNotificationData(
            Guid withdrawalId,
            WalletWithdrawStatus status,
            Guid userId,
            string? reason = null)
        {
            var data = new Dictionary<string, string>
            {
                ["withdrawalId"] = withdrawalId.ToString(),
                ["status"] = status.ToString(),
                ["userId"] = userId.ToString()
            };

            if (!string.IsNullOrWhiteSpace(reason))
            {
                data["reason"] = reason;
            }

            return data;
        }

        private static Transaction CreateWithdrawalTransaction(WalletWithdraw withdrawal)
        {
            return new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = withdrawal.UserId,
                ReferenceId = withdrawal.Id,
                Amount = withdrawal.Amount,
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
