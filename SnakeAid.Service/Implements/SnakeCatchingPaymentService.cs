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

public class SnakeCatchingPaymentService : ISnakeCatchingPaymentService
{
    private readonly IPaymentGateway _paymentGateway;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SnakeCatchingPaymentService> _logger;
    private const string DefaultItemName = "Snake Catching Service";
    private const string LogPrefix = "[SnakeCatchingPaymentService]";
    private readonly string systemId = "57288b98-5f91-4de8-b827-866e3df69587";
    private static readonly Regex OrderCodeRegex = new(@"^SNAKEAID-(\d+)", RegexOptions.Compiled);
    private readonly int commissionFee = 200000;

    public SnakeCatchingPaymentService(
        IPaymentGateway paymentGateway,
        IUnitOfWork unitOfWork,
        IOptions<PayOsOptions> options,
        ILogger<SnakeCatchingPaymentService> logger)
    {
        _paymentGateway = paymentGateway;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<SnakeCatchingPaymentResponse> CreateSnakeCatchingPaymentLinkAsync(
        CreateSnakeCatchingPaymentRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken)
    {
        var paymentResult = await CreateSnakeCatchingPaymentLinkInternalAsync(request, currentUserId, cancellationToken);

        if (!paymentResult.Success)
        {
            throw new InvalidOperationException(paymentResult.ErrorMessage ?? "Failed to create snake catching payment link");
        }

        return new SnakeCatchingPaymentResponse
        {
            SnakeCatchingRequestId = paymentResult.ReferenceId,
            TransactionId = paymentResult.TransactionId,
            Amount = paymentResult.Amount,
            Status = paymentResult.Status,
            CheckoutUrl = paymentResult.CheckoutUrl,
            OrderCode = paymentResult.OrderCode,
            PaymentLinkId = paymentResult.PaymentLinkId,
            ExpiresAt = paymentResult.ExpiresAt,
            Provider = paymentResult.Provider,
            GatewayRawResponse = paymentResult.GatewayRawResponse
        };
    }

    public async Task<CancelPaymentLinkResponse> CancelSnakeCatchingPaymentLinkAsync(
        long orderCode,
        CancelPaymentLinkRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("{Prefix} Cancelling snake catching payment link for OrderCode {OrderCode}", LogPrefix, orderCode);

        var descriptionPattern = $"SNAKEAID-{orderCode}";
        var transaction = await _unitOfWork.GetRepository<Transaction>()
            .FirstOrDefaultAsync(
                predicate: t => t.Description != null && t.Description.StartsWith(descriptionPattern),
                asNoTracking: false,
                cancellationToken: cancellationToken);

        if (transaction == null)
        {
            throw new InvalidOperationException($"Transaction with orderCode {orderCode} not found");
        }

        var providerResult = await _paymentGateway.CancelPaymentLinkAsync(
            orderCode,
            request.CancellationReason,
            cancellationToken);

        if (!providerResult.Success)
        {
            throw new InvalidOperationException($"Failed to cancel payment link {orderCode} on PayOS: {providerResult.ErrorMessage}");
        }

        _unitOfWork.GetRepository<Transaction>().Delete(transaction);
        await _unitOfWork.CommitAsync();

        return new CancelPaymentLinkResponse
        {
            OrderCode = orderCode,
            Status = providerResult.Status,
            Amount = Convert.ToInt32(Math.Round(providerResult.Amount, MidpointRounding.AwayFromZero)),
            AmountPaid = providerResult.AmountPaid,
            AmountRemaining = providerResult.AmountRemaining,
            Message = "Snake catching payment link cancelled successfully"
        };
    }

    public async Task<PayOsWebhookResponse> ProcessSnakeCatchingWebhookAsync(
        string rawPayload,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawPayload))
        {
            throw new ArgumentException("Webhook payload cannot be empty", nameof(rawPayload));
        }

        var webhook = _paymentGateway.VerifyWebhook(rawPayload);
        var result = await ProcessSnakeCatchingWebhookInternalAsync(rawPayload, cancellationToken);

        return new PayOsWebhookResponse
        {
            Success = result.Success,
            Message = result.Success ? "Snake catching webhook processed successfully" : (result.ErrorMessage ?? "Snake catching webhook failed"),
            TransactionId = result.TransactionId,
            OrderCode = webhook.OrderCode,
            Amount = Convert.ToInt32(Math.Round(result.Amount, MidpointRounding.AwayFromZero)),
            TransactionReference = webhook.TransactionReference,
            TransactionDateTime = webhook.TransactionDateTime
        };
    }

    public async Task<PayOsWebhookResponse> ConfirmSnakeCatchingPaymentAsync(
        Guid transactionId,
        CancellationToken cancellationToken)
    {
        var result = await ConfirmSnakeCatchingPaymentInternalAsync(transactionId, cancellationToken);

        return new PayOsWebhookResponse
        {
            Success = result.Success,
            Message = result.Success ? "Snake catching payment confirmed successfully" : (result.ErrorMessage ?? "Snake catching payment confirmation failed"),
            TransactionId = transactionId,
            OrderCode = result.OrderCode,
            Amount = Convert.ToInt32(Math.Round(result.Amount, MidpointRounding.AwayFromZero)),
            TransactionReference = result.OrderCode > 0 ? $"SNAKEAID-{result.OrderCode}" : string.Empty,
            TransactionDateTime = DateTime.UtcNow
        };
    }

    public async Task<PayOsWebhookResponse> ConfirmSnakeCatchingPaymentByOrderCodeAsync(
        long orderCode,
        CancellationToken cancellationToken)
    {
        var descriptionPattern = $"SNAKEAID-{orderCode}";
        var transaction = await _unitOfWork.GetRepository<Transaction>()
            .FirstOrDefaultAsync(
                predicate: t => t.Description != null && t.Description.StartsWith(descriptionPattern),
                asNoTracking: true,
                cancellationToken: cancellationToken);

        if (transaction == null)
        {
            throw new InvalidOperationException($"Transaction with orderCode {orderCode} not found");
        }

        return await ConfirmSnakeCatchingPaymentAsync(transaction.Id, cancellationToken);
    }

    private async Task<SnakeCatchingPaymentOperationResult> CreateSnakeCatchingPaymentLinkInternalAsync(
        CreateSnakeCatchingPaymentRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken)
    {
        try
        {
            var requestId = request.SnakeCatchingRequestId;
            var transactionType = request.TransactionType;
            var senderId = currentUserId;
            var amount = request.Amount;
            var paymentDescription = $"Snake catching payment - {requestId}";

            _logger.LogInformation("{Prefix} Creating payment link for ReferenceId {RequestId}",
                LogPrefix, requestId);

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
                    .GetByIdAsync(requestId);

                if (catchingRequest == null)
                {
                    throw new InvalidOperationException($"SnakeCatchingRequest {requestId} not found");
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
            if (amount <= 0)
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
                    predicate: t => t.ReferenceId == requestId &&
                                   t.TransactionType == transactionType,
                    asNoTracking: false,
                    cancellationToken: cancellationToken);

            if (existingTransaction != null)
            {
                // If transaction already has ExternalTransactionId, it means payment was completed
                if (!string.IsNullOrEmpty(existingTransaction.ExternalTransactionId))
                {
                    throw new InvalidOperationException(
                        $"Payment already completed for request {requestId}");
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
                        await _paymentGateway.CancelPaymentLinkAsync(oldOrderCode, "Creating new payment link for retry", cancellationToken);
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
            var description = BuildDescription(orderCode, paymentDescription);

            // Create Transaction record (Pending)
            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = senderId,
                ReferenceId = requestId,
                Amount = amount,
                Currency = "VND",
                TransactionType = transactionType,
                Description = description,  // Contains orderCode
                PaymentMethod = "PayOS",
                ExternalTransactionId = null,  // Will be updated on webhook
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.GetRepository<Transaction>().InsertAsync(transaction);

            // Create PayOS payment link
            var payOsResult = await _paymentGateway.CreatePaymentLinkAsync(
                new PayOsCreatePaymentRequest
                {
                    OrderCode = orderCode,
                    Amount = amount,
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

            return new SnakeCatchingPaymentOperationResult
            {
                ReferenceId = requestId,
                TransactionId = transaction.Id,
                Amount = amount,
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
                LogPrefix, request.SnakeCatchingRequestId);

            return new SnakeCatchingPaymentOperationResult
            {
                ReferenceId = request.SnakeCatchingRequestId,
                Success = false,
                ErrorMessage = ex.Message,
                GatewayRawResponse = ex
            };
        }
    }

    private async Task<SnakeCatchingPaymentOperationResult> ProcessSnakeCatchingWebhookInternalAsync(
        string rawWebhookPayload,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawWebhookPayload))
        {
            throw new ArgumentException("Webhook payload cannot be empty", nameof(rawWebhookPayload));
        }

        _logger.LogInformation("{Prefix} Processing snake catching PayOS webhook", LogPrefix);

        try
        {
            var webhook = _paymentGateway.VerifyWebhook(rawWebhookPayload);

            return await ProcessWebhookCoreAsync(webhook, triggeredManually: false, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Prefix} Failed to process snake catching webhook", LogPrefix);

            return new SnakeCatchingPaymentOperationResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                GatewayRawResponse = ex
            };
        }
    }

    private async Task<SnakeCatchingPaymentOperationResult> ConfirmSnakeCatchingPaymentInternalAsync(
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
            var linkInfo = await _paymentGateway.GetPaymentLinkInformationAsync(orderCode, cancellationToken);
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

            return new SnakeCatchingPaymentOperationResult
            {
                TransactionId = transactionId,
                Success = false,
                ErrorMessage = ex.Message,
                GatewayRawResponse = ex
            };
        }
    }

    public async Task<TransferToRescuerResponse> TransferSnakeCatchingFundsToRescuerAsync(
        TransferToRescuerRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("{Prefix} [TransferSnakeCatchingFundsToRescuer] Processing transfer for SnakeCatchingRequest {RequestId}",
                LogPrefix, request.SnakeCatchingRequestId);

            var catchingRequest = await _unitOfWork.GetRepository<SnakeCatchingRequest>()
                .GetByIdAsync(request.SnakeCatchingRequestId);

            if (catchingRequest == null)
            {
                throw new InvalidOperationException($"SnakeCatchingRequest {request.SnakeCatchingRequestId} not found");
            }

            if (!catchingRequest.AssignedRescuerId.HasValue)
            {
                throw new InvalidOperationException(
                    $"SnakeCatchingRequest {request.SnakeCatchingRequestId} has no assigned rescuer");
            }

            var rescuerId = catchingRequest.AssignedRescuerId.Value;
            var systemAccountId = Guid.Parse(systemId);

            var existingPayout = await _unitOfWork.GetRepository<Transaction>()
                .FirstOrDefaultAsync(
                    predicate: t => t.ReferenceId == request.SnakeCatchingRequestId &&
                                   t.TransactionType == TransactionType.CatcherPayout &&
                                   t.PaymentMethod == "Internal",
                    asNoTracking: true,
                    cancellationToken: cancellationToken);

            if (existingPayout != null)
            {
                var systemWalletCurrent = await _unitOfWork.GetRepository<Wallet>()
                    .FirstOrDefaultAsync(
                        predicate: w => w.UserId == systemAccountId,
                        asNoTracking: true,
                        cancellationToken: cancellationToken);

                var rescuerWalletCurrent = await _unitOfWork.GetRepository<Wallet>()
                    .FirstOrDefaultAsync(
                        predicate: w => w.UserId == rescuerId,
                        asNoTracking: true,
                        cancellationToken: cancellationToken);

                return new TransferToRescuerResponse
                {
                    Success = true,
                    Message = "Snake catching funds already transferred to rescuer",
                    SnakeCatchingRequestId = request.SnakeCatchingRequestId,
                    RescuerId = rescuerId,
                    TotalAmount = existingPayout.Amount + commissionFee,
                    CommissionFee = commissionFee,
                    NetAmountToRescuer = existingPayout.Amount,
                    TransferTransactionId = existingPayout.Id,
                    SystemWalletBalanceBefore = systemWalletCurrent?.Balance ?? 0,
                    SystemWalletBalanceAfter = systemWalletCurrent?.Balance ?? 0,
                    RescuerWalletBalanceBefore = rescuerWalletCurrent?.Balance ?? 0,
                    RescuerWalletBalanceAfter = rescuerWalletCurrent?.Balance ?? 0,
                    TransferredAt = existingPayout.CreatedAt ?? DateTime.UtcNow
                };
            }

            var paidTransactions = await _unitOfWork.GetRepository<Transaction>()
                .GetListAsync(
                    predicate: t => t.ReferenceId == request.SnakeCatchingRequestId &&
                                   t.ExternalTransactionId != null &&
                                   (t.TransactionType == TransactionType.CatchingDeposit ||
                                   t.TransactionType == TransactionType.CatchingPayment),
                    asNoTracking: false,
                    cancellationToken: cancellationToken);

            if (paidTransactions == null || !paidTransactions.Any())
            {
                throw new InvalidOperationException(
                    $"No paid transactions found for SnakeCatchingRequest {request.SnakeCatchingRequestId}");
            }

            var totalAmount = paidTransactions.Sum(t => t.Amount);
            var netAmountToRescuer = totalAmount - commissionFee;
            if (netAmountToRescuer <= 0)
            {
                throw new InvalidOperationException(
                    $"Invalid payout amount for request {request.SnakeCatchingRequestId}. Total={totalAmount}, Commission={commissionFee}, Net={netAmountToRescuer}");
            }

            var systemWallet = await _unitOfWork.GetRepository<Wallet>()
                .FirstOrDefaultAsync(
                    predicate: w => w.UserId == systemAccountId,
                    asNoTracking: false,
                    cancellationToken: cancellationToken);

            if (systemWallet == null)
            {
                throw new InvalidOperationException($"System wallet for account {systemAccountId} not found");
            }

            if (systemWallet.Balance < netAmountToRescuer)
            {
                throw new InvalidOperationException(
                    $"Insufficient balance in system wallet. Required: {netAmountToRescuer}, Available: {systemWallet.Balance}");
            }

            var systemBalanceBefore = systemWallet.Balance;

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
            }

            var rescuerBalanceBefore = rescuerWallet.Balance;

            systemWallet.Balance -= netAmountToRescuer;
            rescuerWallet.Balance += netAmountToRescuer;

            _unitOfWork.GetRepository<Wallet>().Update(systemWallet);
            _unitOfWork.GetRepository<Wallet>().Update(rescuerWallet);

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

            catchingRequest.Status = RequestStatus.Completed;
            _unitOfWork.GetRepository<SnakeCatchingRequest>().Update(catchingRequest);

            await _unitOfWork.CommitAsync();

            return new TransferToRescuerResponse
            {
                Success = true,
                Message = "Snake catching transfer completed successfully",
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
            _logger.LogError(ex, "{Prefix} [TransferSnakeCatchingFundsToRescuer] Failed to transfer for request {RequestId}",
                LogPrefix, request.SnakeCatchingRequestId);
            throw;
        }
    }

    public async Task<RefundTransactionResponse> RefundSnakeCatchingTransactionAsync(
        RefundTransactionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request.Amount <= 0)
            {
                throw new InvalidOperationException("Refund amount must be greater than 0");
            }

            var receiverExists = await _unitOfWork.GetRepository<Account>()
                .ExistsAsync(a => a.Id == request.ReceiverId, cancellationToken);

            if (!receiverExists)
            {
                throw new InvalidOperationException($"Receiver account {request.ReceiverId} not found");
            }

            var systemAccountId = Guid.Parse(systemId);
            var systemWallet = await _unitOfWork.GetRepository<Wallet>()
                .FirstOrDefaultAsync(
                    predicate: w => w.UserId == systemAccountId,
                    asNoTracking: false,
                    cancellationToken: cancellationToken);

            if (systemWallet == null)
            {
                throw new InvalidOperationException($"System wallet for account {systemAccountId} not found");
            }

            if (systemWallet.Balance < request.Amount)
            {
                throw new InvalidOperationException(
                    $"Insufficient balance in system wallet. Required: {request.Amount}, Available: {systemWallet.Balance}");
            }

            var systemBalanceBefore = systemWallet.Balance;

            var receiverWallet = await _unitOfWork.GetRepository<Wallet>()
                .FirstOrDefaultAsync(
                    predicate: w => w.UserId == request.ReceiverId,
                    asNoTracking: false,
                    cancellationToken: cancellationToken);

            if (receiverWallet == null)
            {
                receiverWallet = new Wallet
                {
                    Id = Guid.NewGuid(),
                    UserId = request.ReceiverId,
                    Balance = 0
                };
                await _unitOfWork.GetRepository<Wallet>().InsertAsync(receiverWallet);
            }

            var receiverBalanceBefore = receiverWallet.Balance;

            systemWallet.Balance -= request.Amount;
            receiverWallet.Balance += request.Amount;

            _unitOfWork.GetRepository<Wallet>().Update(systemWallet);
            _unitOfWork.GetRepository<Wallet>().Update(receiverWallet);

            var systemWithdrawTransaction = new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = systemAccountId,
                ReferenceId = request.ReferenceId,
                Amount = request.Amount,
                Currency = "VND",
                TransactionType = TransactionType.WalletWithdraw,
                Description = $"Refund to receiver {request.ReceiverId}: {request.Description}",
                PaymentMethod = "Internal",
                ExternalTransactionId = $"REFUND-{Guid.NewGuid()}",
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.GetRepository<Transaction>().InsertAsync(systemWithdrawTransaction);

            var refundTransaction = new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = request.ReceiverId,
                ReferenceId = request.ReferenceId,
                Amount = request.Amount,
                Currency = "VND",
                TransactionType = request.TransactionType,
                Description = $"Refund: {request.Description}",
                PaymentMethod = "Internal",
                ExternalTransactionId = $"REFUND-{Guid.NewGuid()}",
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.GetRepository<Transaction>().InsertAsync(refundTransaction);

            await _unitOfWork.CommitAsync();

            return new RefundTransactionResponse
            {
                Success = true,
                Message = "Snake catching refund completed successfully",
                ReceiverId = request.ReceiverId,
                RefundAmount = request.Amount,
                RefundTransactionId = refundTransaction.Id,
                SystemWalletBalanceBefore = systemBalanceBefore,
                SystemWalletBalanceAfter = systemWallet.Balance,
                ReceiverWalletBalanceBefore = receiverBalanceBefore,
                ReceiverWalletBalanceAfter = receiverWallet.Balance,
                RefundedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Prefix} [RefundSnakeCatchingTransaction] Failed to refund for receiver {ReceiverId}",
                LogPrefix, request.ReceiverId);
            throw;
        }
    }

    private async Task<SnakeCatchingPaymentOperationResult> ProcessWebhookCoreAsync(
        PayOsWebhookData webhook,
        bool triggeredManually,
        CancellationToken cancellationToken)
    {
        var sourceTag = triggeredManually ? "[ManualConfirm]" : "[Webhook]";
        _logger.LogWarning(
            "{Prefix}{SourceTag} <<< PAYOS EVENT RECEIVED >>> OrderCode={OrderCode}, Success={Success}, Amount={Amount}, TransactionRef={TransactionRef}",
            LogPrefix, sourceTag, webhook.OrderCode, webhook.Success, webhook.Amount, webhook.TransactionReference);

        Guid transactionId = Guid.Empty;
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

                // Handle different transaction types
                if (transaction.TransactionType == TransactionType.CatchingPayment)
                {
                    await HandleCatcherCommissionAsync(transaction, sourceTag, cancellationToken);
                }
                else if (transaction.TransactionType == TransactionType.WalletTopup)
                {
                    await HandleWalletTopupAsync(transaction, sourceTag, cancellationToken);
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

            return new SnakeCatchingPaymentOperationResult
            {
                ReferenceId = transaction.ReferenceId,
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

            return new SnakeCatchingPaymentOperationResult
            {
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

    private async Task HandleWalletTopupAsync(Transaction transaction, string sourceTag, CancellationToken cancellationToken)
    {
        try
        {
            // For wallet top-up, we credit the user's wallet directly
            var userId = transaction.UserId;
            var topupAmount = transaction.Amount;

            // Get or create user wallet
            var userWallet = await _unitOfWork.GetRepository<Wallet>()
                .FirstOrDefaultAsync(
                    predicate: w => w.UserId == userId,
                    asNoTracking: false,
                    cancellationToken: cancellationToken);

            if (userWallet == null)
            {
                userWallet = new Wallet
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Balance = 0
                };
                await _unitOfWork.GetRepository<Wallet>().InsertAsync(userWallet);
                _logger.LogInformation("{Prefix}{SourceTag} Created wallet for user {UserId}",
                    LogPrefix, sourceTag, userId);
            }

            var previousBalance = userWallet.Balance;
            userWallet.Balance += topupAmount;
            _unitOfWork.GetRepository<Wallet>().Update(userWallet);

            _logger.LogInformation("{Prefix}{SourceTag} Wallet top-up completed. UserId={UserId}, Amount={Amount}, Balance: {PrevBalance} -> {NewBalance}",
                LogPrefix, sourceTag, userId, topupAmount, previousBalance, userWallet.Balance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Prefix}{SourceTag} Failed to process wallet top-up for transaction {TransactionId}",
                LogPrefix, sourceTag, transaction.Id);
            // Don't throw - wallet top-up failure shouldn't fail the payment processing
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

    private sealed class SnakeCatchingPaymentOperationResult
    {
        public Guid ReferenceId { get; init; }
        public Guid TransactionId { get; init; }
        public decimal Amount { get; init; }
        public string Status { get; init; } = string.Empty;
        public string CheckoutUrl { get; init; } = string.Empty;
        public long OrderCode { get; init; }
        public string PaymentLinkId { get; init; } = string.Empty;
        public DateTime? ExpiresAt { get; init; }
        public string Provider { get; init; } = "PayOS";
        public object? GatewayRawResponse { get; init; }
        public bool Success { get; init; }
        public string? ErrorMessage { get; init; }
    }
}
