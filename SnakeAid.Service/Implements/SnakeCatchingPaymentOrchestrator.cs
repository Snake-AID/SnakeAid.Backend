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

namespace SnakeAid.Service.Implements;

public class SnakeCatchingPaymentOrchestrator : IPaymentOrchestrator
{
    private readonly IPayOsProvider _payOsProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly PayOsOptions _options;
    private readonly ILogger<SnakeCatchingPaymentOrchestrator> _logger;
    private const string DefaultItemName = "Snake Catching Service";
    private const string LogPrefix = "[SnakeCatchingOrchestrator]";
    private readonly string systemId = "57288b98-5f91-4de8-b827-866e3df69587";
    private static readonly Regex OrderCodeRegex = new(@"^SNAKEAID-(\d+)", RegexOptions.Compiled);
    private readonly int commissionFee = 200000;

    public SnakeCatchingPaymentOrchestrator(
        IPayOsProvider payOsProvider,
        IUnitOfWork unitOfWork,
        IOptions<PayOsOptions> options,
        ILogger<SnakeCatchingPaymentOrchestrator> logger)
    {
        _payOsProvider = payOsProvider;
        _unitOfWork = unitOfWork;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<PaymentResult> CreatePaymentLinkAsync(
        PaymentContext context,
        CancellationToken cancellationToken)
    {
        // Validate this is for snake catching
        if (context.ReferenceType != PaymentReferenceType.SnakeCatching)
        {
            throw new InvalidOperationException($"{LogPrefix} Only handles SnakeCatching payments, got {context.ReferenceType}");
        }

        try
        {
            _logger.LogInformation("{Prefix} Creating payment link for ReferenceId {RequestId}",
                LogPrefix, context.ReferenceId);

            // Extract transaction type from metadata
            var transactionType = context.Metadata?.TryGetValue("TransactionType", out var type) == true
                ? Enum.Parse<TransactionType>(type.ToString()!)
                : TransactionType.CatchingPayment;

            // SenderId is the current user
            var senderId = context.SenderId;

            // ReceiverId is always the system account
            var receiverId = Guid.Parse(systemId);

            // Validate SnakeCatchingRequest only for snake catching related transaction types
            var isSnakeCatchingTransaction = transactionType == TransactionType.CatchingPayment ||
                                            transactionType == TransactionType.CatchingDeposit ||
                                            transactionType == TransactionType.CatchingRefund ||
                                            transactionType == TransactionType.CatcherPayout;

            if (isSnakeCatchingTransaction)
            {
                // Validate SnakeCatchingRequest exists
                var catchingRequest = await _unitOfWork.GetRepository<SnakeCatchingRequest>()
                    .GetByIdAsync(context.ReferenceId);

                if (catchingRequest == null)
                {
                    throw new InvalidOperationException($"SnakeCatchingRequest {context.ReferenceId} not found");
                }

                // Validate status based on transaction type
                if (transactionType == TransactionType.CatchingDeposit)
                {
                    // Deposit (travel fee) can be paid at initial stages before assignment
                    if (catchingRequest.Status != RequestStatus.Pending &&
                        catchingRequest.Status != RequestStatus.Confirmed &&
                        catchingRequest.Status != RequestStatus.Assigned)
                    {
                        throw new InvalidOperationException(
                            $"Cannot create deposit payment for request with status {catchingRequest.Status}. " +
                            "Request must be Pending, Confirmed, or Assigned.");
                    }
                }
                else
                {
                    // Other snake catching payments should be made after assignment or completion
                    if (catchingRequest.Status != RequestStatus.Assigned &&
                        catchingRequest.Status != RequestStatus.Finished)
                    {
                        throw new InvalidOperationException(
                            $"Cannot create payment for request with status {catchingRequest.Status}. " +
                            "Request must be Assigned or Finished.");
                    }
                }
            }

            // Validate amount
            if (context.Amount <= 0)
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
                    predicate: t => t.ReferenceId == context.ReferenceId &&
                                   t.TransactionType == transactionType,
                    asNoTracking: false,
                    cancellationToken: cancellationToken);

            if (existingTransaction != null)
            {
                // If transaction already has ExternalTransactionId, it means payment was completed
                if (!string.IsNullOrEmpty(existingTransaction.ExternalTransactionId))
                {
                    throw new InvalidOperationException(
                        $"Payment already completed for request {context.ReferenceId}");
                }

                // If no ExternalTransactionId, payment was not completed - allow retry
                _logger.LogInformation("{Prefix} Found pending unpaid transaction {TransactionId}. Cancelling old payment link and allowing retry.",
                    LogPrefix, existingTransaction.Id);

                // Try to cancel old payment link on PayOS
                try
                {
                    var oldOrderCode = ExtractOrderCodeFromDescription(existingTransaction.Description);
                    if (oldOrderCode > 0)
                    {
                        _logger.LogInformation("{Prefix} Cancelling old PayOS payment link with orderCode {OrderCode}",
                            LogPrefix, oldOrderCode);
                        await _payOsProvider.CancelPaymentLinkAsync(oldOrderCode, "Creating new payment link for retry", cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    // If cancel fails (e.g., link already expired), just log and continue
                    _logger.LogWarning(ex, "{Prefix} Failed to cancel old payment link, continuing with new payment creation", LogPrefix);
                }

                // Delete old transaction to allow creating new one
                _unitOfWork.GetRepository<Transaction>().Delete(existingTransaction);
                await _unitOfWork.CommitAsync();

                _logger.LogInformation("{Prefix} Deleted old pending transaction, allowing new payment creation", LogPrefix);
            }

            // Generate orderCode
            var orderCode = GenerateOrderCode();
            var description = BuildDescription(orderCode, context.Description);

            // Create Transaction record (Pending)
            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = senderId,
                ReferenceId = context.ReferenceId,
                Amount = context.Amount,
                Currency = "VND",
                TransactionType = transactionType,
                Description = description,  // Contains orderCode
                PaymentMethod = "PayOS",
                ExternalTransactionId = null,  // Will be updated on webhook
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.GetRepository<Transaction>().InsertAsync(transaction);

            // Create PayOS payment link
            var payOsResult = await _payOsProvider.CreatePaymentLinkAsync(
                new PayOsCreatePaymentRequest
                {
                    OrderCode = orderCode,
                    Amount = context.Amount,
                    Description = description,
                    ItemName = DefaultItemName,
                    Quantity = 1
                },
                cancellationToken);

            if (!payOsResult.Success)
            {
                throw new InvalidOperationException($"Failed to create PayOS payment link: {payOsResult.ErrorMessage}");
            }

            await _unitOfWork.CommitAsync();

            _logger.LogInformation("{Prefix} Payment link created. TransactionId={TransactionId}, TransactionType={TransactionType}, OrderCode={OrderCode}, CheckoutUrl={CheckoutUrl}",
                LogPrefix, transaction.Id, transactionType, orderCode, payOsResult.CheckoutUrl);

            return new PaymentResult
            {
                ReferenceId = context.ReferenceId,
                ReferenceType = PaymentReferenceType.SnakeCatching,
                TransactionId = transaction.Id,
                Amount = context.Amount,
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
                },
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Prefix} Failed to create payment link for ReferenceId {RequestId}",
                LogPrefix, context.ReferenceId);

            return new PaymentResult
            {
                ReferenceId = context.ReferenceId,
                ReferenceType = PaymentReferenceType.SnakeCatching,
                Success = false,
                ErrorMessage = ex.Message,
                GatewayRawResponse = ex
            };
        }
    }

    public async Task<PaymentResult> ProcessWebhookAsync(
        Guid referenceId,
        PaymentReferenceType referenceType,
        string rawWebhookPayload,
        CancellationToken cancellationToken)
    {
        // Validate this is for snake catching
        if (referenceType != PaymentReferenceType.SnakeCatching)
        {
            throw new InvalidOperationException($"{LogPrefix} Only handles SnakeCatching payments, got {referenceType}");
        }

        if (string.IsNullOrWhiteSpace(rawWebhookPayload))
        {
            throw new ArgumentException("Webhook payload cannot be empty", nameof(rawWebhookPayload));
        }

        _logger.LogInformation("{Prefix} Processing PayOS webhook for ReferenceId {ReferenceId}", LogPrefix, referenceId);

        try
        {
            var webhook = _payOsProvider.VerifyWebhook(rawWebhookPayload);

            return await ProcessWebhookCoreAsync(webhook, triggeredManually: false, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Prefix} Failed to process webhook for ReferenceId {ReferenceId}", LogPrefix, referenceId);

            return new PaymentResult
            {
                ReferenceId = referenceId,
                ReferenceType = PaymentReferenceType.SnakeCatching,
                Success = false,
                ErrorMessage = ex.Message,
                GatewayRawResponse = ex
            };
        }
    }

    public async Task<PaymentResult> ConfirmPaymentAsync(
        Guid transactionId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("{Prefix} Confirming payment for Transaction {TransactionId}",
            LogPrefix, transactionId);

        try
        {
            var transaction = await _unitOfWork.GetRepository<Transaction>()
                .GetByIdAsync(transactionId);

            if (transaction == null)
            {
                throw new InvalidOperationException($"Transaction {transactionId} not found");
            }

            // Extract orderCode from description
            var orderCode = ExtractOrderCodeFromDescription(transaction.Description);
            if (orderCode == 0)
            {
                throw new InvalidOperationException(
                    $"Cannot extract orderCode from transaction description: {transaction.Description}");
            }

            // Get payment info from PayOS
            var linkInfo = await _payOsProvider.GetPaymentLinkInformationAsync(orderCode, cancellationToken);
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Prefix} Failed to confirm payment for Transaction {TransactionId}", LogPrefix, transactionId);

            return new PaymentResult
            {
                ReferenceId = Guid.Empty,
                ReferenceType = PaymentReferenceType.SnakeCatching,
                TransactionId = transactionId,
                Success = false,
                ErrorMessage = ex.Message,
                GatewayRawResponse = ex
            };
        }
    }

    private async Task<PaymentResult> ProcessWebhookCoreAsync(
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
                                   t.Description.StartsWith(descriptionPattern),
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

                // Update SnakeCatchingRequest status to Paid only for CatchingPayment transactions
                if (transaction.TransactionType == TransactionType.CatchingPayment)
                {
                    var catchingRequest = await _unitOfWork.GetRepository<SnakeCatchingRequest>()
                        .GetByIdAsync(transaction.ReferenceId);

                    if (catchingRequest != null)
                    {
                        catchingRequest.Status = RequestStatus.Paid;
                        _unitOfWork.GetRepository<SnakeCatchingRequest>().Update(catchingRequest);

                        _logger.LogInformation("{Prefix}{SourceTag} SnakeCatchingRequest {RequestId} status updated to Paid",
                            LogPrefix, sourceTag, transaction.ReferenceId);
                    }
                }

                // Handle commission and payouts for completed catching requests
                if (transaction.TransactionType == TransactionType.CatchingPayment)
                {
                    await HandleCatcherCommissionAsync(transaction, sourceTag, cancellationToken);
                }
            }
            else
            {
                _logger.LogWarning("{Prefix}{SourceTag} Payment failed. TransactionId={TransactionId}, Code={Code}, Description={Description}",
                    LogPrefix, sourceTag, transactionId, webhook.Code, webhook.Description);
            }

            await _unitOfWork.CommitAsync();

            _logger.LogInformation("{Prefix}{SourceTag} Webhook processing completed. TransactionId={TransactionId}, Success={Success}",
                LogPrefix, sourceTag, transactionId, webhook.Success);

            return new PaymentResult
            {
                ReferenceId = transaction.ReferenceId,
                ReferenceType = PaymentReferenceType.SnakeCatching,
                TransactionId = transactionId,
                Amount = webhook.Amount,
                Status = webhook.Success ? "Paid" : "Failed",
                Provider = "PayOS",
                GatewayRawResponse = webhook,
                Success = webhook.Success,
                ErrorMessage = webhook.Success ? null : $"{webhook.Code}: {webhook.Description}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Prefix}{SourceTag} Failed to process webhook for OrderCode {OrderCode}",
                LogPrefix, sourceTag, webhook.OrderCode);

            return new PaymentResult
            {
                ReferenceId = Guid.Empty,
                ReferenceType = PaymentReferenceType.SnakeCatching,
                TransactionId = transactionId,
                Success = false,
                ErrorMessage = ex.Message,
                GatewayRawResponse = ex
            };
        }
    }

    private async Task HandleCatcherCommissionAsync(Transaction transaction, string sourceTag, CancellationToken cancellationToken)
    {
        try
        {
            var catchingRequest = await _unitOfWork.GetRepository<SnakeCatchingRequest>()
                .FirstOrDefaultAsync(
                    predicate: scr => scr.Id == transaction.ReferenceId,
                    include: query => query.Include(scr => scr.AssignedRescuer),
                    cancellationToken: cancellationToken);

            if (catchingRequest?.AssignedRescuer == null)
            {
                _logger.LogWarning("{Prefix}{SourceTag} No assigned rescuer found for catching request {RequestId}",
                    LogPrefix, sourceTag, transaction.ReferenceId);
                return;
            }

            var rescuerId = catchingRequest.AssignedRescuerId.Value;
            var commissionAmount = Math.Min(commissionFee, transaction.Amount);

            // Deduct commission from system wallet
            var systemAccountId = Guid.Parse(systemId);
            var systemWallet = await _unitOfWork.GetRepository<Wallet>()
                .FirstOrDefaultAsync(
                    predicate: w => w.UserId == systemAccountId,
                    asNoTracking: false,
                    cancellationToken: cancellationToken);

            if (systemWallet == null || systemWallet.Balance < commissionAmount)
            {
                _logger.LogWarning("{Prefix}{SourceTag} Insufficient system wallet balance for commission payout. Balance={Balance}, Commission={Commission}",
                    LogPrefix, sourceTag, systemWallet?.Balance ?? 0, commissionAmount);
                return;
            }

            var previousSystemBalance = systemWallet.Balance;
            systemWallet.Balance -= commissionAmount;
            _unitOfWork.GetRepository<Wallet>().Update(systemWallet);

            // Add commission to rescuer wallet
            var rescuerWallet = await _unitOfWork.GetRepository<Wallet>()
                .FirstOrDefaultAsync(
                    predicate: w => w.UserId == rescuerId,
                    asNoTracking: false,
                    cancellationToken: cancellationToken);

            if (rescuerWallet == null)
            {
                rescuerWallet = new Wallet
                {
                    Id = Guid.NewGuid(),
                    UserId = rescuerId,
                    Balance = 0
                };
                await _unitOfWork.GetRepository<Wallet>().InsertAsync(rescuerWallet);
                _logger.LogInformation("{Prefix}{SourceTag} Created wallet for rescuer {RescuerId}",
                    LogPrefix, sourceTag, rescuerId);
            }

            var previousRescuerBalance = rescuerWallet.Balance;
            rescuerWallet.Balance += commissionAmount;
            _unitOfWork.GetRepository<Wallet>().Update(rescuerWallet);

            // Create commission transaction records
            var commissionTransaction = new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = rescuerId,
                ReferenceId = transaction.ReferenceId,
                Amount = commissionAmount,
                Currency = transaction.Currency,
                TransactionType = TransactionType.CatcherPayout,
                Description = $"Commission payout for catching request {transaction.ReferenceId}",
                PaymentMethod = "Internal",
                ExternalTransactionId = transaction.ExternalTransactionId,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.GetRepository<Transaction>().InsertAsync(commissionTransaction);

            _logger.LogInformation("{Prefix}{SourceTag} Commission payout completed. Amount={Amount}, Rescuer={RescuerId}, SystemBalance: {PrevSysBalance} -> {NewSysBalance}, RescuerBalance: {PrevResBalance} -> {NewResBalance}",
                LogPrefix, sourceTag, commissionAmount, rescuerId, previousSystemBalance, systemWallet.Balance, previousRescuerBalance, rescuerWallet.Balance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Prefix}{SourceTag} Failed to process catcher commission for transaction {TransactionId}",
                LogPrefix, sourceTag, transaction.Id);
            // Don't throw - commission failure shouldn't fail the payment
        }
    }

    private long GenerateOrderCode()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var random = new Random();
        var randomPart = random.Next(1000, 9999);
        return long.Parse($"{timestamp}{randomPart}");
    }

    private string BuildDescription(long orderCode, string? customDescription)
    {
        var baseDescription = $"SNAKEAID-{orderCode}";
        return string.IsNullOrEmpty(customDescription)
            ? baseDescription
            : $"{baseDescription} - {customDescription}";
    }

    private long ExtractOrderCodeFromDescription(string description)
    {
        if (string.IsNullOrEmpty(description))
            return 0;

        var match = OrderCodeRegex.Match(description);
        return match.Success && long.TryParse(match.Groups[1].Value, out var orderCode)
            ? orderCode
            : 0;
    }

    private bool IsPaymentLinkPaid(PayOsLinkInformation linkInfo)
    {
        return linkInfo.Status.Equals("PAID", StringComparison.OrdinalIgnoreCase) ||
               (linkInfo.AmountPaid > 0 && linkInfo.AmountPaid >= linkInfo.Amount);
    }
}