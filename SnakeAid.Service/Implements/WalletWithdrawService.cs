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
using System.Data;
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

            WalletWithdraw? withdrawal = null;
            var executionStrategy = _unitOfWork.Context.Database.CreateExecutionStrategy();
            await executionStrategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _unitOfWork.Context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
                try
                {
                    var wallet = await _unitOfWork.Context.Set<Wallet>()
                        .SingleOrDefaultAsync(w => w.UserId == userId);

                    if (wallet == null)
                    {
                        throw new NotFoundException($"Wallet not found for user with ID: {userId}");
                    }

                    var pendingAmounts = await _unitOfWork.Context.Set<WalletWithdraw>()
                        .Where(w => w.UserId == userId && w.Status == WalletWithdrawStatus.Pending)
                        .Select(w => w.Amount)
                        .ToListAsync();
                    var pendingAmount = pendingAmounts.Sum();

                    var availableBalance = wallet.Balance - pendingAmount;
                    if (availableBalance < amount)
                    {
                        throw new ConflictException(
                            "Insufficient wallet balance",
                            WithdrawalErrorCodes.WithdrawalInsufficientBalance);
                    }

                    var startOfTodayUtc = DateTime.UtcNow.Date;
                    var endOfTodayUtc = startOfTodayUtc.AddDays(1);
                    var todayAmounts = await _unitOfWork.Context.Set<WalletWithdraw>()
                        .Where(w =>
                            w.UserId == userId &&
                            w.CreatedAt >= startOfTodayUtc &&
                            w.CreatedAt < endOfTodayUtc &&
                            w.Status != WalletWithdrawStatus.Rejected &&
                            w.Status != WalletWithdrawStatus.Failed)
                        .Select(w => w.Amount)
                        .ToListAsync();
                    var todayTotal = todayAmounts.Sum();

                    if (todayTotal + amount > DailyWithdrawalLimit)
                    {
                        throw new ConflictException(
                            $"Daily withdrawal limit is {DailyWithdrawalLimit:N0} VND",
                            WithdrawalErrorCodes.WithdrawalDailyLimitExceeded);
                    }

                    withdrawal = new WalletWithdraw
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

                    await _unitOfWork.GetRepository<WalletWithdraw>().InsertAsync(withdrawal);
                    await _unitOfWork.CommitAsync();
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    await _unitOfWork.RollbackAsync();
                    throw;
                }
            });

            if (withdrawal == null)
            {
                throw new InvalidOperationException("Withdrawal creation did not complete.");
            }

            await TryBroadcastAdminNotificationAsync(new AdminBroadcastNotificationRequest
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

        public async Task<WalletWithdraw?> GetWithdrawalByIdAsync(Guid withdrawalId)
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
            var withdrawal = await ExecuteWithdrawalMutationAsync(async () =>
            {
                var trackedWithdrawal = await _unitOfWork.GetRepository<WalletWithdraw>()
                    .FirstOrDefaultAsync(
                        predicate: w => w.Id == withdrawalId,
                        asNoTracking: false);

                if (trackedWithdrawal == null)
                {
                    throw new NotFoundException(
                        "Withdrawal not found",
                        WithdrawalErrorCodes.WithdrawalNotFound);
                }

                if (trackedWithdrawal.UserId != userId)
                {
                    throw new ForbiddenException(
                        "You are not allowed to access this withdrawal",
                        WithdrawalErrorCodes.WithdrawalForbidden);
                }

                if (trackedWithdrawal.Status != WalletWithdrawStatus.Pending)
                {
                    throw new ConflictException(
                        "Can only cancel pending withdrawals",
                        WithdrawalErrorCodes.WithdrawalInvalidStatus);
                }

                trackedWithdrawal.Status = WalletWithdrawStatus.Rejected;
                trackedWithdrawal.RejectionReason = "Cancelled by user";
                trackedWithdrawal.ProcessedAt = DateTime.UtcNow;

                return trackedWithdrawal;
            });

            await TryBroadcastAdminNotificationAsync(new AdminBroadcastNotificationRequest
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
            var withdrawal = await ExecuteWithdrawalMutationAsync(async () =>
            {
                var trackedWithdrawal = await _unitOfWork.GetRepository<WalletWithdraw>()
                    .FirstOrDefaultAsync(
                        predicate: w => w.Id == withdrawalId,
                        include: q => q.Include(w => w.Wallet),
                        asNoTracking: false
                    );

                if (trackedWithdrawal == null)
                {
                    throw new NotFoundException(
                        "Withdrawal not found",
                        WithdrawalErrorCodes.WithdrawalNotFound);
                }

                if (trackedWithdrawal.Status != WalletWithdrawStatus.Pending)
                {
                    throw new ConflictException(
                        "Can only approve pending withdrawals",
                        WithdrawalErrorCodes.WithdrawalInvalidStatus);
                }

                if (string.IsNullOrWhiteSpace(trackedWithdrawal.BankBin))
                {
                    throw new ConflictException(
                        "Withdrawal bank BIN is missing and QR cannot be generated",
                        WithdrawalErrorCodes.WithdrawalBankBinMissing);
                }

                if (trackedWithdrawal.Wallet.Balance < trackedWithdrawal.Amount)
                {
                    throw new ConflictException(
                        "Insufficient wallet balance to approve withdrawal",
                        WithdrawalErrorCodes.WithdrawalInsufficientBalance);
                }

                var (payload, imageBase64) = _vietQrAdapter.GenerateQr(
                    trackedWithdrawal.BankBin,
                    trackedWithdrawal.BankAccount,
                    trackedWithdrawal.AccountHolderName,
                    trackedWithdrawal.Amount,
                    $"Withdrawal {trackedWithdrawal.Id}");

                trackedWithdrawal.Wallet.Balance -= trackedWithdrawal.Amount;
                trackedWithdrawal.Status = WalletWithdrawStatus.Approved;
                trackedWithdrawal.ProcessedByAdminId = adminUserId;
                trackedWithdrawal.AdminNotes = NormalizeText(adminNotes);
                trackedWithdrawal.VietQrPayload = payload;
                trackedWithdrawal.VietQrImageBase64 = imageBase64;
                trackedWithdrawal.ProcessedAt = DateTime.UtcNow;

                await _unitOfWork.GetRepository<Transaction>().InsertAsync(CreateWithdrawalTransaction(trackedWithdrawal));
                return trackedWithdrawal;
            });

            await TryPublishUserNotificationAsync(
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
            var withdrawal = await ExecuteWithdrawalMutationAsync(async () =>
            {
                var trackedWithdrawal = await _unitOfWork.GetRepository<WalletWithdraw>()
                    .FirstOrDefaultAsync(
                        predicate: w => w.Id == withdrawalId,
                        include: q => q.Include(w => w.Wallet),
                        asNoTracking: false
                    );
                if (trackedWithdrawal == null)
                {
                    throw new NotFoundException(
                        "Withdrawal not found",
                        WithdrawalErrorCodes.WithdrawalNotFound);
                }

                if (trackedWithdrawal.Status != WalletWithdrawStatus.Pending && trackedWithdrawal.Status != WalletWithdrawStatus.Approved)
                {
                    throw new ConflictException(
                        "Can only reject pending or approved withdrawals",
                        WithdrawalErrorCodes.WithdrawalInvalidStatus);
                }

                if (trackedWithdrawal.Status == WalletWithdrawStatus.Approved)
                {
                    trackedWithdrawal.Wallet.Balance += trackedWithdrawal.Amount;
                    await _unitOfWork.GetRepository<Transaction>().InsertAsync(
                        CreateWithdrawalRefundTransaction(trackedWithdrawal, "Withdrawal rejected after approval"));
                }

                trackedWithdrawal.Status = WalletWithdrawStatus.Rejected;
                trackedWithdrawal.ProcessedByAdminId = adminUserId;
                trackedWithdrawal.RejectionReason = reason;
                trackedWithdrawal.AdminNotes = NormalizeText(adminNotes);
                trackedWithdrawal.VietQrPayload = null;
                trackedWithdrawal.VietQrImageBase64 = null;
                trackedWithdrawal.ProcessedAt = DateTime.UtcNow;

                return trackedWithdrawal;
            });

            await TryPublishUserNotificationAsync(
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
            var withdrawal = await ExecuteWithdrawalMutationAsync(async () =>
            {
                var trackedWithdrawal = await _unitOfWork.GetRepository<WalletWithdraw>()
                    .FirstOrDefaultAsync(
                        predicate: w => w.Id == withdrawalId,
                        include: q => q.Include(w => w.Wallet),
                        asNoTracking: false
                    );

                if (trackedWithdrawal == null)
                {
                    throw new NotFoundException(
                        "Withdrawal not found",
                        WithdrawalErrorCodes.WithdrawalNotFound);
                }

                if (trackedWithdrawal.Status != WalletWithdrawStatus.Approved)
                {
                    throw new ConflictException(
                        "Can only complete approved withdrawals",
                        WithdrawalErrorCodes.WithdrawalInvalidStatus);
                }

                trackedWithdrawal.Status = WalletWithdrawStatus.Completed;
                trackedWithdrawal.ProcessedByAdminId = adminUserId;
                trackedWithdrawal.AdminNotes = NormalizeText(adminNotes) ?? trackedWithdrawal.AdminNotes;
                trackedWithdrawal.ProcessedAt = DateTime.UtcNow;

                return trackedWithdrawal;
            });

            await TryPublishUserNotificationAsync(
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
            var withdrawal = await ExecuteWithdrawalMutationAsync(async () =>
            {
                var trackedWithdrawal = await _unitOfWork.GetRepository<WalletWithdraw>()
                    .FirstOrDefaultAsync(
                        predicate: w => w.Id == withdrawalId,
                        include: q => q.Include(w => w.Wallet),
                        asNoTracking: false
                    );
                if (trackedWithdrawal == null)
                {
                    throw new NotFoundException(
                        "Withdrawal not found",
                        WithdrawalErrorCodes.WithdrawalNotFound);
                }

                if (trackedWithdrawal.Status != WalletWithdrawStatus.Approved)
                {
                    throw new ConflictException(
                        "Can only fail approved withdrawals",
                        WithdrawalErrorCodes.WithdrawalInvalidStatus);
                }

                trackedWithdrawal.Wallet.Balance += trackedWithdrawal.Amount;
                trackedWithdrawal.Status = WalletWithdrawStatus.Failed;
                trackedWithdrawal.ProcessedByAdminId = adminUserId;
                trackedWithdrawal.RejectionReason = reason;
                trackedWithdrawal.AdminNotes = NormalizeText(adminNotes);
                trackedWithdrawal.VietQrPayload = null;
                trackedWithdrawal.VietQrImageBase64 = null;
                trackedWithdrawal.ProcessedAt = DateTime.UtcNow;

                await _unitOfWork.GetRepository<Transaction>().InsertAsync(
                    CreateWithdrawalRefundTransaction(trackedWithdrawal, "Withdrawal failed after approval"));
                return trackedWithdrawal;
            });

            await TryPublishUserNotificationAsync(
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

        private async Task<T> ExecuteWithdrawalMutationAsync<T>(Func<Task<T>> operation)
        {
            var executionStrategy = _unitOfWork.Context.Database.CreateExecutionStrategy();
            try
            {
                return await executionStrategy.ExecuteAsync(async () =>
                {
                    await using var transaction = await _unitOfWork.Context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
                    try
                    {
                        var result = await operation();
                        await _unitOfWork.CommitAsync();
                        await transaction.CommitAsync();
                        return result;
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        await _unitOfWork.RollbackAsync();
                        throw;
                    }
                });
            }
            catch (Exception ex) when (IsWithdrawalConcurrencyConflict(ex))
            {
                throw new ConflictException(
                    "Withdrawal was updated by another operation. Please reload and try again.",
                    WithdrawalErrorCodes.WithdrawalInvalidStatus);
            }
        }

        private static bool IsWithdrawalConcurrencyConflict(Exception exception)
        {
            if (exception is DbUpdateConcurrencyException)
            {
                return true;
            }

            var postgresException = FindExceptionByTypeName(exception, "Npgsql.PostgresException");
            if (postgresException != null)
            {
                var sqlState = postgresException.GetType().GetProperty("SqlState")?.GetValue(postgresException)?.ToString();
                if (sqlState is "40001" or "40P01")
                {
                    return true;
                }
            }

            return false;
        }

        private static Exception? FindExceptionByTypeName(Exception exception, string fullTypeName)
        {
            Exception? current = exception;
            while (current != null)
            {
                if (string.Equals(current.GetType().FullName, fullTypeName, StringComparison.Ordinal))
                {
                    return current;
                }

                current = current.InnerException;
            }

            return null;
        }

        private async Task TryPublishUserNotificationAsync(
            Guid userId,
            string title,
            string body,
            string type,
            Dictionary<string, string> data)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish user notification {NotificationType} for user {UserId}", type, userId);
            }
        }

        private async Task TryBroadcastAdminNotificationAsync(AdminBroadcastNotificationRequest request)
        {
            try
            {
                await _notificationQueueService.BroadcastAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to broadcast admin notification {NotificationType}", request.Type);
            }
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
