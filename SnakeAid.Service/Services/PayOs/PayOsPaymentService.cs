using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Requests.PayOs;
using SnakeAid.Core.Responses.PayOs;
using SnakeAid.Core.Settings;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;
using SnakeAid.Service.Services.PayOs.Models;

namespace SnakeAid.Service.Services.PayOs;

public class PayOsPaymentService : IPayOsPaymentService
{
    private readonly IPayOsClient _payOsClient;
    private readonly IUnitOfWork _unitOfWork;
    private readonly PayOsOptions _options;
    private readonly ILogger<PayOsPaymentService> _logger;
    private const string DefaultItemName = "Snake Catching Service";
    private const string LogPrefix = "[PayOS]";
    private readonly string systemId = "57288b98-5f91-4de8-b827-866e3df69587";
    private static readonly Regex OrderCodeRegex = new(@"^SNAKEAID-(\d+)", RegexOptions.Compiled);
    private readonly int commissionFee = 20000;

    public PayOsPaymentService(
        IPayOsClient payOsClient,
        IUnitOfWork unitOfWork,
        IOptions<PayOsOptions> options,
        ILogger<PayOsPaymentService> logger)
    {
        _payOsClient = payOsClient;
        _unitOfWork = unitOfWork;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<SnakeCatchingPaymentResponse> CreatePaymentLinkAsync(
        CreateSnakeCatchingPaymentRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("{Prefix} Creating payment link for SnakeCatchingRequest {RequestId}", 
                LogPrefix, request.SnakeCatchingRequestId);

            // SenderId is the current user
            var senderId = currentUserId;
            
            // ReceiverId is always the system account
            var receiverId = Guid.Parse(systemId);

            // Validate SnakeCatchingRequest exists
            var catchingRequest = await _unitOfWork.GetRepository<SnakeCatchingRequest>()
                .GetByIdAsync(request.SnakeCatchingRequestId);

            if (catchingRequest == null)
            {
                throw new InvalidOperationException($"SnakeCatchingRequest {request.SnakeCatchingRequestId} not found");
            }

            // Validate status - must be Assigned or Finished
            if (catchingRequest.Status != RequestStatus.Assigned && 
                catchingRequest.Status != RequestStatus.Finished)
            {
                throw new InvalidOperationException(
                    $"Cannot create payment for request with status {catchingRequest.Status}. " +
                    "Request must be Assigned or Finished.");
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
                                   t.TransactionType == TransactionType.CatchingPayment,
                    asNoTracking: false,
                    cancellationToken: cancellationToken);

            if (existingTransaction != null)
            {
                throw new InvalidOperationException(
                    $"Payment transaction already exists for request {request.SnakeCatchingRequestId}");
            }

            // Generate orderCode
            var orderCode = GenerateOrderCode();
            var description = BuildDescription(orderCode, request.Description);

            // Create Transaction record (Pending)
            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = senderId,
                ReferenceId = request.SnakeCatchingRequestId,
                Amount = request.Amount,
                Currency = "VND",
                TransactionType = TransactionType.CatchingPayment,
                Description = description,  // Contains orderCode
                PaymentMethod = "PayOS",
                ExternalTransactionId = null,  // Will be updated on webhook
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.GetRepository<Transaction>().InsertAsync(transaction);

            // Store receiverId temporarily in memory or session
            // Since we can't add fields to Transaction, we'll store it in a static dictionary
            // or retrieve it from SnakeCatchingRequest.AssignedRescuerId later
            // For now, we'll use the AssignedRescuerId from the request

            // Create PayOS payment link
            var amountInt = Convert.ToInt32(Math.Round(request.Amount, MidpointRounding.AwayFromZero));
            var payOsResult = await _payOsClient.CreatePaymentLinkAsync(
                new PayOsLinkCreateContext
                {
                    OrderCode = orderCode,
                    Amount = amountInt,
                    Description = description,
                    CancelUrl = _options.CancelUrl,
                    ReturnUrl = _options.ReturnUrl,
                    Items = new[]
                    {
                        new PayOsItemPayload
                        {
                            Name = DefaultItemName,
                            Quantity = 1,
                            Price = amountInt
                        }
                    }
                },
                cancellationToken);

            await _unitOfWork.CommitAsync();

            _logger.LogInformation("{Prefix} Payment link created. TransactionId={TransactionId}, OrderCode={OrderCode}, CheckoutUrl={CheckoutUrl}",
                LogPrefix, transaction.Id, orderCode, payOsResult.CheckoutUrl);

            return new SnakeCatchingPaymentResponse
            {
                TransactionId = transaction.Id,
                SnakeCatchingRequestId = request.SnakeCatchingRequestId,
                Amount = request.Amount,
                Status = "Pending",
                CheckoutUrl = payOsResult.CheckoutUrl,
                OrderCode = orderCode,
                PaymentLinkId = payOsResult.PaymentLinkId,
                ExpiresAt = null,
                Provider = "PayOS",
                GatewayRawResponse = new
                {
                    payOsResult.OrderCode,
                    payOsResult.PaymentLinkId,
                    payOsResult.CheckoutUrl,
                    payOsResult.Amount,
                    payOsResult.Status,
                    payOsResult.Currency
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Prefix} Failed to create payment link for SnakeCatchingRequest {RequestId}",
                LogPrefix, request.SnakeCatchingRequestId);
            throw;
        }
    }

    public async Task<CancelPaymentLinkResponse> CancelPaymentLinkAsync(
        long orderCode,
        CancelPaymentLinkRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("{Prefix} Cancelling payment link for OrderCode {OrderCode}", LogPrefix, orderCode);

            // Find transaction by orderCode in Description
            var descriptionPattern = $"SNAKEAID-{orderCode}";
            var transaction = await _unitOfWork.GetRepository<Transaction>()
                .FirstOrDefaultAsync(
                    predicate: t => t.Description != null && 
                                   t.Description.StartsWith(descriptionPattern) &&
                                   t.TransactionType == TransactionType.CatchingPayment,
                    asNoTracking: false,
                    cancellationToken: cancellationToken);

            if (transaction == null)
            {
                throw new InvalidOperationException($"Transaction with orderCode {orderCode} not found");
            }

            // Cancel on PayOS
            var payOsResult = await _payOsClient.CancelPaymentLinkAsync(
                orderCode,
                request.CancellationReason,
                cancellationToken);

            if (payOsResult == null)
            {
                throw new InvalidOperationException($"Failed to cancel payment link {orderCode} on PayOS");
            }

            // Delete transaction (or you can keep it and mark as cancelled in description)
            _unitOfWork.GetRepository<Transaction>().Delete(transaction);
            await _unitOfWork.CommitAsync();

            _logger.LogInformation("{Prefix} Payment link cancelled. OrderCode={OrderCode}", LogPrefix, orderCode);

            return new CancelPaymentLinkResponse
            {
                OrderCode = orderCode,
                Status = payOsResult.Status,
                Amount = payOsResult.Amount,
                AmountPaid = payOsResult.AmountPaid,
                AmountRemaining = payOsResult.AmountRemaining,
                Message = "Payment link cancelled successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Prefix} Failed to cancel payment link {OrderCode}", LogPrefix, orderCode);
            throw;
        }
    }

    public async Task<PayOsWebhookResponse> ProcessWebhookAsync(
        string rawPayload,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawPayload))
        {
            throw new ArgumentException("Webhook payload cannot be empty", nameof(rawPayload));
        }

        _logger.LogInformation("{Prefix} [Webhook] Processing PayOS webhook", LogPrefix);

        var webhook = _payOsClient.VerifyWebhook(rawPayload);

        return await ProcessWebhookCoreAsync(webhook, triggeredManually: false, cancellationToken);
    }

    public async Task<PayOsWebhookResponse> ConfirmPaymentAsync(
        Guid transactionId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("{Prefix} [ManualConfirm] Confirming payment for Transaction {TransactionId}",
            LogPrefix, transactionId);

        var transaction = await _unitOfWork.GetRepository<Transaction>()
            .GetByIdAsync(transactionId);

        if (transaction == null)
        {
            throw new InvalidOperationException($"Transaction {transactionId} not found");
        }

        if (transaction.TransactionType != TransactionType.CatchingPayment)
        {
            throw new InvalidOperationException(
                $"Transaction {transactionId} is not a CatchingPayment transaction");
        }

        // Extract orderCode from description
        var orderCode = ExtractOrderCodeFromDescription(transaction.Description);
        if (orderCode == 0)
        {
            throw new InvalidOperationException(
                $"Cannot extract orderCode from transaction description: {transaction.Description}");
        }

        // Get payment info from PayOS
        var linkInfo = await _payOsClient.GetPaymentLinkInformationAsync(orderCode, cancellationToken);
        if (linkInfo == null)
        {
            throw new InvalidOperationException($"Unable to retrieve payment information for orderCode {orderCode}");
        }

        if (!IsPaymentLinkPaid(linkInfo))
        {
            throw new InvalidOperationException(
                $"PayOS reports status '{linkInfo.Status}'. Payment cannot be confirmed.");
        }

        // Build webhook data from link info
        var webhookData = new PayOsWebhookData
        {
            Success = true,
            Code = "00",
            Description = "Manual confirmation",
            OrderCode = orderCode,
            Amount = linkInfo.Amount,
            PaymentLinkId = linkInfo.Id,
            TransactionReference = $"MANUAL-{transactionId}",
            TransactionDateTime = DateTime.UtcNow
        };

        return await ProcessWebhookCoreAsync(webhookData, triggeredManually: true, cancellationToken);
    }

    public async Task<PayOsWebhookResponse> ConfirmPaymentByOrderCodeAsync(
        long orderCode,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("{Prefix} [ConfirmByOrderCode] Confirming payment for OrderCode {OrderCode}",
            LogPrefix, orderCode);

        try
        {
            // Find transaction by orderCode in description
            var descriptionPattern = $"SNAKEAID-{orderCode}";
            var transaction = await _unitOfWork.GetRepository<Transaction>()
                .FirstOrDefaultAsync(
                    predicate: t => t.Description != null &&
                                   t.Description.StartsWith(descriptionPattern) &&
                                   t.TransactionType == TransactionType.CatchingPayment,
                    asNoTracking: true,
                    cancellationToken: cancellationToken);

            if (transaction == null)
            {
                throw new InvalidOperationException($"Transaction with orderCode {orderCode} not found");
            }

            _logger.LogInformation("{Prefix} [ConfirmByOrderCode] Found transaction {TransactionId}, confirming...",
                LogPrefix, transaction.Id);

            // Delegate to existing ConfirmPaymentAsync
            return await ConfirmPaymentAsync(transaction.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Prefix} [ConfirmByOrderCode] Failed to confirm payment for OrderCode {OrderCode}",
                LogPrefix, orderCode);
            throw;
        }
    }

    private async Task<PayOsWebhookResponse> ProcessWebhookCoreAsync(
        PayOsWebhookData webhook,
        bool triggeredManually,
        CancellationToken cancellationToken)
    {
        var sourceTag = triggeredManually ? "[ManualConfirm]" : "[Webhook]";
        _logger.LogWarning(
            "{Prefix}{SourceTag} <<< PAYOS EVENT RECEIVED >>> OrderCode={OrderCode}, Success={Success}, Amount={Amount}, TransactionRef={TransactionRef}",
            LogPrefix, sourceTag, webhook.OrderCode, webhook.Success, webhook.Amount, webhook.TransactionReference);

        Guid transactionId = Guid.Empty;
        Guid? payoutTransactionId = null;

        try
        {
            // Find transaction by orderCode in Description
            var descriptionPattern = $"SNAKEAID-{webhook.OrderCode}";
            var transaction = await _unitOfWork.GetRepository<Transaction>()
                .FirstOrDefaultAsync(
                    predicate: t => t.Description != null &&
                                   t.Description.StartsWith(descriptionPattern) &&
                                   t.TransactionType == TransactionType.CatchingPayment,
                    include: query => query.Include(t => t.User),
                    asNoTracking: false,
                    cancellationToken: cancellationToken);

            if (transaction == null)
            {
                throw new InvalidOperationException($"Transaction with orderCode {webhook.OrderCode} not found");
            }

            transactionId = transaction.Id;

            if (webhook.Success)
            {
                _logger.LogInformation("{Prefix}{SourceTag} Payment successful. TransactionId={TransactionId}",
                    LogPrefix, sourceTag, transactionId);

                // Update transaction with PayOS reference
                transaction.ExternalTransactionId = webhook.TransactionReference;
                transaction.CreatedAt = webhook.TransactionDateTime ?? DateTime.UtcNow;
                _unitOfWork.GetRepository<Transaction>().Update(transaction);

                // Add amount to system wallet
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
                    _logger.LogInformation("{Prefix}{SourceTag} Created system wallet for account {AccountId}",
                        LogPrefix, sourceTag, systemAccountId);
                }

                var previousBalance = systemWallet.Balance;
                systemWallet.Balance += transaction.Amount;
                _unitOfWork.GetRepository<Wallet>().Update(systemWallet);

                // Create transaction record for system wallet receiving payment
                var systemWalletTransaction = new Transaction
                {
                    Id = Guid.NewGuid(),
                    UserId = systemAccountId,
                    ReferenceId = transaction.ReferenceId, // Same SnakeCatchingRequestId
                    Amount = transaction.Amount,
                    Currency = transaction.Currency,
                    TransactionType = TransactionType.WalletTopup,
                    Description = $"Received payment for catching request {transaction.ReferenceId}",
                    PaymentMethod = "PayOS",
                    ExternalTransactionId = webhook.TransactionReference,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.GetRepository<Transaction>().InsertAsync(systemWalletTransaction);

                _logger.LogInformation("{Prefix}{SourceTag} System wallet updated. Amount={Amount}, Balance: {PrevBalance} -> {NewBalance}, TransactionId={TransactionId}",
                    LogPrefix, sourceTag, transaction.Amount, previousBalance, systemWallet.Balance, systemWalletTransaction.Id);

                // Update SnakeCatchingRequest status to Paid
                var catchingRequest = await _unitOfWork.GetRepository<SnakeCatchingRequest>()
                    .GetByIdAsync(transaction.ReferenceId);

                if (catchingRequest != null)
                {
                    catchingRequest.Status = RequestStatus.Paid;
                    _unitOfWork.GetRepository<SnakeCatchingRequest>().Update(catchingRequest);

                    _logger.LogInformation("{Prefix}{SourceTag} SnakeCatchingRequest {RequestId} status updated to Paid",
                        LogPrefix, sourceTag, transaction.ReferenceId);
                }

                await _unitOfWork.CommitAsync();

                return new PayOsWebhookResponse
                {
                    Success = true,
                    Message = "Payment processed successfully",
                    TransactionId = transactionId,
                    PayoutTransactionId = payoutTransactionId,
                    OrderCode = webhook.OrderCode,
                    Amount = webhook.Amount,
                    TransactionReference = webhook.TransactionReference,
                    TransactionDateTime = webhook.TransactionDateTime
                };
            }
            else
            {
                _logger.LogWarning("{Prefix}{SourceTag} Payment failed. TransactionId={TransactionId}, Code={Code}, Description={Description}",
                    LogPrefix, sourceTag, transactionId, webhook.Code, webhook.Description);

                // Delete or mark transaction as failed
                _unitOfWork.GetRepository<Transaction>().Delete(transaction);
                await _unitOfWork.CommitAsync();

                return new PayOsWebhookResponse
                {
                    Success = false,
                    Message = $"Payment failed: {webhook.Description}",
                    TransactionId = transactionId,
                    OrderCode = webhook.OrderCode,
                    Amount = webhook.Amount,
                    TransactionReference = webhook.TransactionReference,
                    TransactionDateTime = webhook.TransactionDateTime
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Prefix}{SourceTag} Error processing webhook for OrderCode {OrderCode}",
                LogPrefix, sourceTag, webhook.OrderCode);
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
        // PayOS limits description to 25 characters max
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

    private static long ExtractOrderCodeFromDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return 0;
        }

        var match = OrderCodeRegex.Match(description);
        if (match.Success && long.TryParse(match.Groups[1].Value, out var orderCode))
        {
            return orderCode;
        }

        return 0;
    }

    private static bool IsPaymentLinkPaid(PayOsLinkInformation linkInfo)
    {
        return linkInfo.Status.Equals("PAID", StringComparison.OrdinalIgnoreCase) ||
               linkInfo.AmountPaid >= linkInfo.Amount;
    }

    public async Task<TransferToRescuerResponse> TransferToRescuerAsync(
        TransferToRescuerRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("{Prefix} [TransferToRescuer] Processing transfer for SnakeCatchingRequest {RequestId}",
                LogPrefix, request.SnakeCatchingRequestId);

            // Validate SnakeCatchingRequest exists
            var catchingRequest = await _unitOfWork.GetRepository<SnakeCatchingRequest>()
                .GetByIdAsync(request.SnakeCatchingRequestId);

            if (catchingRequest == null)
            {
                throw new InvalidOperationException($"SnakeCatchingRequest {request.SnakeCatchingRequestId} not found");
            }

            // Validate AssignedRescuerId exists
            if (!catchingRequest.AssignedRescuerId.HasValue)
            {
                throw new InvalidOperationException(
                    $"SnakeCatchingRequest {request.SnakeCatchingRequestId} has no assigned rescuer");
            }

            var rescuerId = catchingRequest.AssignedRescuerId.Value;
            var systemAccountId = Guid.Parse(systemId);

            // Get all paid transactions for this catching request
            var paidTransactions = await _unitOfWork.GetRepository<Transaction>()
                .GetListAsync(
                    predicate: t => t.ReferenceId == request.SnakeCatchingRequestId &&
                                   t.TransactionType == TransactionType.CatchingPayment &&
                                   t.ExternalTransactionId != null,  // Only paid transactions
                    asNoTracking: false,
                    cancellationToken: cancellationToken);

            if (paidTransactions == null || !paidTransactions.Any())
            {
                throw new InvalidOperationException(
                    $"No paid transactions found for SnakeCatchingRequest {request.SnakeCatchingRequestId}");
            }

            // Calculate total amount and commission
            var totalAmount = paidTransactions.Sum(t => t.Amount);
            var netAmountToRescuer = totalAmount - commissionFee;

            _logger.LogInformation("{Prefix} [TransferToRescuer] Found {Count} paid transactions. Total={Total}, Commission={Commission} ({Rate}%), NetAmount={Net}",
                LogPrefix, paidTransactions.Count(), totalAmount, commissionFee, netAmountToRescuer);

            // Get or create system wallet
            var systemWallet = await _unitOfWork.GetRepository<Wallet>()
                .FirstOrDefaultAsync(
                    predicate: w => w.UserId == systemAccountId,
                    asNoTracking: false,
                    cancellationToken: cancellationToken);

            if (systemWallet == null)
            {
                throw new InvalidOperationException($"System wallet for account {systemAccountId} not found");
            }

            // Check if system wallet has enough balance (only need net amount, commission stays in system)
            if (systemWallet.Balance < netAmountToRescuer)
            {
                throw new InvalidOperationException(
                    $"Insufficient balance in system wallet. Required: {netAmountToRescuer}, Available: {systemWallet.Balance}");
            }

            var systemBalanceBefore = systemWallet.Balance;

            // Get or create rescuer wallet
            var rescuerWallet = await _unitOfWork.GetRepository<Wallet>()
                .FirstOrDefaultAsync(
                    predicate: w => w.UserId == rescuerId,
                    asNoTracking: false,
                    cancellationToken: cancellationToken);

            if (rescuerWallet == null)
            {
                // Create wallet if not exists
                rescuerWallet = new Wallet
                {
                    Id = Guid.NewGuid(),
                    UserId = rescuerId,
                    Balance = 0
                };
                await _unitOfWork.GetRepository<Wallet>().InsertAsync(rescuerWallet);
                _logger.LogInformation("{Prefix} [TransferToRescuer] Created new wallet for rescuer {RescuerId}",
                    LogPrefix, rescuerId);
            }

            var rescuerBalanceBefore = rescuerWallet.Balance;

            // Update wallet balances (only transfer net amount, commission stays in system wallet)
            systemWallet.Balance -= netAmountToRescuer;
            rescuerWallet.Balance += netAmountToRescuer;

            _unitOfWork.GetRepository<Wallet>().Update(systemWallet);
            _unitOfWork.GetRepository<Wallet>().Update(rescuerWallet);

            // Create transaction for system wallet withdrawal
            var systemWithdrawTransaction = new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = systemAccountId,
                ReferenceId = request.SnakeCatchingRequestId,
                Amount = netAmountToRescuer,
                Currency = "VND",
                TransactionType = TransactionType.WalletWithdraw,
                Description = $"Transfer to rescuer {rescuerId} for request {request.SnakeCatchingRequestId}",
                PaymentMethod = "Internal",
                ExternalTransactionId = $"WITHDRAW-{Guid.NewGuid()}",
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.GetRepository<Transaction>().InsertAsync(systemWithdrawTransaction);

            // Create transaction for platform commission fee
            var commissionTransaction = new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = systemAccountId,
                ReferenceId = request.SnakeCatchingRequestId,
                Amount = commissionFee,
                Currency = "VND",
                TransactionType = TransactionType.PlatformFee,
                Description = $"Platform commission for request {request.SnakeCatchingRequestId}",
                PaymentMethod = "Internal",
                ExternalTransactionId = $"COMMISSION-{Guid.NewGuid()}",
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.GetRepository<Transaction>().InsertAsync(commissionTransaction);

            // Create transfer transaction record for rescuer wallet
            var transferTransaction = new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = rescuerId,
                ReferenceId = request.SnakeCatchingRequestId,
                Amount = netAmountToRescuer,
                Currency = "VND",
                TransactionType = TransactionType.CatcherPayout,
                Description = $"Payout for request {request.SnakeCatchingRequestId}",
                PaymentMethod = "Internal",
                ExternalTransactionId = $"TRANSFER-{Guid.NewGuid()}",
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.GetRepository<Transaction>().InsertAsync(transferTransaction);

            await _unitOfWork.CommitAsync();

            _logger.LogInformation(
                "{Prefix} [TransferToRescuer] Successfully transferred {NetAmount} (Total: {Total}, Commission: {Commission}) " +
                "from system ({SystemBalance} -> {SystemBalanceAfter}) to rescuer {RescuerId} ({RescuerBalance} -> {RescuerBalanceAfter}). " +
                "Transactions created: Withdraw={WithdrawTxId}, Commission={CommissionTxId}, Payout={PayoutTxId}",
                LogPrefix, netAmountToRescuer, totalAmount, commissionFee,
                systemBalanceBefore, systemWallet.Balance, rescuerId, rescuerBalanceBefore, rescuerWallet.Balance,
                systemWithdrawTransaction.Id, commissionTransaction.Id, transferTransaction.Id);

            return new TransferToRescuerResponse
            {
                Success = true,
                Message = "Transfer completed successfully",
                SnakeCatchingRequestId = request.SnakeCatchingRequestId,
                RescuerId = rescuerId,
                TotalAmount = totalAmount,
                CommissionFee = commissionFee,
                NetAmountToRescuer = netAmountToRescuer,
                TransferTransactionId = transferTransaction.Id,
                SystemWalletBalanceBefore = systemBalanceBefore,
                SystemWalletBalanceAfter = systemWallet.Balance,
                RescuerWalletBalanceBefore = rescuerBalanceBefore,
                RescuerWalletBalanceAfter = rescuerWallet.Balance,
                TransferredAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Prefix} [TransferToRescuer] Failed to transfer for SnakeCatchingRequest {RequestId}",
                LogPrefix, request.SnakeCatchingRequestId);
            throw;
        }
    }
}
