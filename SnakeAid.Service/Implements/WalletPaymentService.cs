using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Requests.PayOs;
using SnakeAid.Core.Responses.PayOs;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Service.Implements;

public class WalletPaymentService : IWalletPaymentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<WalletPaymentService> _logger;
    private const string LogPrefix = "[WalletPayment]";
    private readonly string systemId = "57288b98-5f91-4de8-b827-866e3df69587";
    private static readonly Regex OrderCodeRegex = new(@"^SNAKEAID-(\d+)", RegexOptions.Compiled);

    public WalletPaymentService(
        IUnitOfWork unitOfWork,
        ILogger<WalletPaymentService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<SnakeCatchingPaymentResponse> CreateWalletPaymentAsync(
        CreateSnakeCatchingPaymentRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("{Prefix} Creating wallet payment for ReferenceId {RequestId}, TransactionType {TransactionType}", 
                LogPrefix, request.SnakeCatchingRequestId, request.TransactionType);

            // SenderId is the current user
            var senderId = currentUserId;
            
            // ReceiverId is always the system account
            var receiverId = Guid.Parse(systemId);

            // Validate SnakeCatchingRequest only for snake catching related transaction types
            var isSnakeCatchingTransaction = request.TransactionType == TransactionType.CatchingPayment ||
                                            request.TransactionType == TransactionType.CatchingDeposit ||
                                            request.TransactionType == TransactionType.CatchingRefund ||
                                            request.TransactionType == TransactionType.CatcherPayout;

            if (isSnakeCatchingTransaction)
            {
                // Validate SnakeCatchingRequest exists
                var catchingRequest = await _unitOfWork.GetRepository<SnakeCatchingRequest>()
                    .GetByIdAsync(request.SnakeCatchingRequestId);

                if (catchingRequest == null)
                {
                    throw new InvalidOperationException($"SnakeCatchingRequest {request.SnakeCatchingRequestId} not found");
                }

                // Validate status - must be OperatorContacting or Finished
                if (catchingRequest.Status != RequestStatus.Pending && 
                    catchingRequest.Status != RequestStatus.OperatorContacting &&
                    catchingRequest.Status != RequestStatus.Finished)
                {
                    throw new InvalidOperationException(
                        $"Cannot create payment for request with status {catchingRequest.Status}. " +
                        "Request must be Assigned or Finished.");
                }
            }

            // Validate amount
            if (request.Amount <= 0)
            {
                throw new InvalidOperationException("Payment amount must be greater than 0");
            }

            // Validate system receiver account exists
            var receiverExists = await _unitOfWork.GetRepository<Account>()
                .ExistsAsync(a => a.Id == receiverId, cancellationToken);

            if (!receiverExists)
            {
                throw new InvalidOperationException($"System receiver account {receiverId} not found");
            }

            // Check if payment already exists for this request
            var existingTransaction = await _unitOfWork.GetRepository<Transaction>()
                .FirstOrDefaultAsync(
                    predicate: t => t.ReferenceId == request.SnakeCatchingRequestId && 
                                   t.TransactionType == request.TransactionType,
                    asNoTracking: false,
                    cancellationToken: cancellationToken);

            if (existingTransaction != null)
            {
                throw new InvalidOperationException(
                    $"Payment transaction already exists for request {request.SnakeCatchingRequestId}");
            }

            // Get user wallet
            var userWallet = await _unitOfWork.GetRepository<Wallet>()
                .FirstOrDefaultAsync(
                    predicate: w => w.UserId == senderId,
                    asNoTracking: false,
                    cancellationToken: cancellationToken);

            if (userWallet == null)
            {
                throw new InvalidOperationException($"User wallet not found for user {senderId}");
            }

            // Check if user has enough balance
            if (userWallet.Balance < request.Amount)
            {
                throw new InvalidOperationException(
                    $"Insufficient wallet balance. Available: {userWallet.Balance} VND, Required: {request.Amount} VND");
            }

            // Generate orderCode
            var orderCode = GenerateOrderCode();
            var description = BuildDescription(orderCode, request.Description);

            // Deduct amount from user wallet
            var userPreviousBalance = userWallet.Balance;
            userWallet.Balance -= request.Amount;
            _unitOfWork.GetRepository<Wallet>().Update(userWallet);

            _logger.LogInformation("{Prefix} User wallet debited. UserId={UserId}, Amount={Amount}, Balance: {PrevBalance} -> {NewBalance}",
                LogPrefix, senderId, request.Amount, userPreviousBalance, userWallet.Balance);

            // Create debit transaction for user (use WalletWithdraw to indicate money going out)
            var userTransaction = new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = senderId,
                ReferenceId = request.SnakeCatchingRequestId,
                Amount = request.Amount, // Positive value, type indicates direction
                Currency = "VND",
                TransactionType = request.TransactionType, // Money leaving user wallet
                Description = $"{description} - Payment for {request.TransactionType}",
                PaymentMethod = "Wallet",
                ExternalTransactionId = $"WALLET-{orderCode}",
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.GetRepository<Transaction>().InsertAsync(userTransaction);

            // Get or create system wallet
            var systemAccountId = Guid.Parse(systemId);
            var systemWallet = await _unitOfWork.GetRepository<Wallet>()
                .FirstOrDefaultAsync(
                    predicate: w => w.UserId == systemAccountId,
                    asNoTracking: false,
                    cancellationToken: cancellationToken);

            if (systemWallet == null)
            {
                // Create system wallet if not exists
                systemWallet = new Wallet
                {
                    Id = Guid.NewGuid(),
                    UserId = systemAccountId,
                    Balance = 0
                };
                await _unitOfWork.GetRepository<Wallet>().InsertAsync(systemWallet);
                _logger.LogInformation("{Prefix} Created system wallet for account {AccountId}",
                    LogPrefix, systemAccountId);
            }

            // Add amount to system wallet
            var systemPreviousBalance = systemWallet.Balance;
            systemWallet.Balance += request.Amount;
            _unitOfWork.GetRepository<Wallet>().Update(systemWallet);

            _logger.LogInformation("{Prefix} System wallet credited. Amount={Amount}, Balance: {PrevBalance} -> {NewBalance}",
                LogPrefix, request.Amount, systemPreviousBalance, systemWallet.Balance);

            // Create credit transaction for system
            var systemTransaction = new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = systemAccountId,
                ReferenceId = request.SnakeCatchingRequestId,
                Amount = request.Amount,
                Currency = "VND",
                TransactionType = TransactionType.WalletTopup,
                Description = $"Received wallet payment for catching request {request.SnakeCatchingRequestId}",
                PaymentMethod = "Wallet",
                ExternalTransactionId = $"WALLET-{orderCode}",
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.GetRepository<Transaction>().InsertAsync(systemTransaction);

            // Update SnakeCatchingRequest status to Confirmed only for CatchingDeposit transactions
            if (request.TransactionType == TransactionType.CatchingDeposit)
            {
                var catchingRequest = await _unitOfWork.GetRepository<SnakeCatchingRequest>()
                    .GetByIdAsync(request.SnakeCatchingRequestId);

                if (catchingRequest != null)
                {
                    catchingRequest.Status = RequestStatus.Confirmed;
                    _unitOfWork.GetRepository<SnakeCatchingRequest>().Update(catchingRequest);

                    _logger.LogInformation("{Prefix} SnakeCatchingRequest {RequestId} status updated to Paid",
                        LogPrefix, request.SnakeCatchingRequestId);
                }
            }

            // Update SnakeCatchingRequest status to Paid only for CatchingPayment transactions
            if (request.TransactionType == TransactionType.CatchingPayment)
            {
                var catchingRequest = await _unitOfWork.GetRepository<SnakeCatchingRequest>()
                    .GetByIdAsync(request.SnakeCatchingRequestId);

                if (catchingRequest != null)
                {
                    catchingRequest.Status = RequestStatus.Paid;
                    _unitOfWork.GetRepository<SnakeCatchingRequest>().Update(catchingRequest);

                    _logger.LogInformation("{Prefix} SnakeCatchingRequest {RequestId} status updated to Paid",
                        LogPrefix, request.SnakeCatchingRequestId);
                }
            }

            await _unitOfWork.CommitAsync();

            _logger.LogInformation("{Prefix} Wallet payment completed successfully. UserTransactionId={UserTransactionId}, SystemTransactionId={SystemTransactionId}, OrderCode={OrderCode}",
                LogPrefix, userTransaction.Id, systemTransaction.Id, orderCode);

            return new SnakeCatchingPaymentResponse
            {
                TransactionId = userTransaction.Id,
                SnakeCatchingRequestId = request.SnakeCatchingRequestId,
                Amount = request.Amount,
                Status = "Paid",
                CheckoutUrl = null, // No checkout URL for wallet payment
                OrderCode = orderCode,
                PaymentLinkId = null,
                ExpiresAt = null,
                Provider = "Wallet",
                GatewayRawResponse = new
                {
                    OrderCode = orderCode,
                    UserTransactionId = userTransaction.Id,
                    SystemTransactionId = systemTransaction.Id,
                    UserWalletBalance = userWallet.Balance,
                    SystemWalletBalance = systemWallet.Balance,
                    PaymentMethod = "Wallet"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Prefix} Failed to create wallet payment for SnakeCatchingRequest {RequestId}",
                LogPrefix, request.SnakeCatchingRequestId);
            throw;
        }
    }

    private static long GenerateOrderCode()
    {
        // Generate a unique order code based on timestamp
        // Format: Unix timestamp (10 digits) + random 3 digits
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var random = new Random().Next(100, 999);
        return long.Parse($"{timestamp}{random}");
    }

    private string BuildDescription(long orderCode, string? additionalInfo)
    {
        // Keep same format as PayOS
        const int maxLength = 25;
        var baseDescription = $"SNAKEAID-{orderCode}";
        
        if (!string.IsNullOrWhiteSpace(additionalInfo))
        {
            // Try to append part of additionalInfo if space allows
            var combined = $"{baseDescription} {additionalInfo}";
            if (combined.Length > maxLength)
            {
                // Truncate to fit within limit
                return combined.Substring(0, maxLength);
            }
            return combined;
        }
        
        return baseDescription.Length > maxLength 
            ? baseDescription.Substring(0, maxLength) 
            : baseDescription;
    }
}
