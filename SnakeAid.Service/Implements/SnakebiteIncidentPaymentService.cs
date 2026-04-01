using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Enums;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Requests.PayOs;
using SnakeAid.Core.Responses.PayOs;
using SnakeAid.Core.Requests.PayOS;
using SnakeAid.Core.Responses.PayOS;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;
using SnakeAid.Service.Services.PayOs.Models;

namespace SnakeAid.Service.Implements;

public class SnakebiteIncidentPaymentService : ISnakebiteIncidentPaymentService
{
    private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
    private readonly IPaymentGateway _paymentGateway;
    private readonly ILogger<SnakebiteIncidentPaymentService> _logger;
    private const string SystemWalletUserId = "57288b98-5f91-4de8-b827-866e3df69587";

    public SnakebiteIncidentPaymentService(
        IUnitOfWork<SnakeAidDbContext> unitOfWork,
        IPaymentGateway paymentGateway,
        ILogger<SnakebiteIncidentPaymentService> logger)
    {
        _unitOfWork = unitOfWork;
        _paymentGateway = paymentGateway;
        _logger = logger;
    }

    public async Task<SnakebiteIncidentPaymentResponse> CreateSnakebiteIncidentPaymentLinkAsync(
        CreateSnakebiteIncidentPaymentRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
        {
            throw new ValidationException("Payment amount must be greater than 0.");
        }

        var incident = await _unitOfWork.GetRepository<SnakebiteIncident>().FirstOrDefaultAsync(
            predicate: i => i.Id == request.SnakebiteIncidentId,
            include: q => q.Include(i => i.Missions).OrderByDescending(i => i.CreatedAt),
            asNoTracking: true,
            cancellationToken: cancellationToken);

        if (incident == null)
        {
            throw new NotFoundException("Snakebite incident was not found.");
        }

        if (incident.UserId != currentUserId)
        {
            throw new ForbiddenException("You are not allowed to create payment link for this incident.");
        }

        if (incident.Status == SnakebiteIncidentStatus.Completed)
        {
            throw new ConflictException("This incident has already been paid.");
        }

        if (incident.Status != SnakebiteIncidentStatus.Finished)
        {
            throw new ConflictException("Payment is allowed only after the rescue mission is completed.");
        }

        var successfulMission = incident.Missions.FirstOrDefault(m => m.Status == RescueMissionStatus.MissionCompleted);
        if (successfulMission == null)
        {
            throw new ConflictException("No completed mission was found for this incident.");
        }

        var expectedAmount = successfulMission.ActualCost ?? successfulMission.Price;
        if (request.Amount != expectedAmount)
        {
            throw new ValidationException($"Payment amount must equal completed mission amount ({expectedAmount}).");
        }

        var pendingTransaction = await PreparePendingPayOsTransactionAsync(
            currentUserId,
            request.SnakebiteIncidentId,
            request.Amount,
            request.Description ?? string.Empty,
            cancellationToken);

        var paymentLink = await _paymentGateway.CreatePaymentLinkAsync(new PayOsCreatePaymentRequest
        {
            OrderCode = pendingTransaction.OrderCode,
            Amount = request.Amount,
            Description = BuildDescription(pendingTransaction.OrderCode, request.Description ?? string.Empty),
            ItemName = "Snakebite Incident Payment",
            Quantity = 1
        }, cancellationToken);

        if (!paymentLink.Success)
        {
            var txToDelete = await FindIncidentTransactionByOrderCodeAsync(pendingTransaction.OrderCode, false, cancellationToken);
            if (txToDelete != null && string.IsNullOrWhiteSpace(txToDelete.ExternalTransactionId))
            {
                _unitOfWork.GetRepository<Transaction>().Delete(txToDelete);
                await _unitOfWork.CommitAsync();
            }

            throw new ValidationException("Payment link creation failed: " + (paymentLink.ErrorMessage ?? "Unknown error"));
        }

        return new SnakebiteIncidentPaymentResponse
        {
            SnakebiteIncidentId = request.SnakebiteIncidentId,
            TransactionId = pendingTransaction.TransactionId,
            OrderCode = paymentLink.OrderCode,
            Amount = request.Amount,
            Currency = "VND",
            Status = "Pending",
            Provider = "PayOS",
            CheckoutUrl = paymentLink.CheckoutUrl,
            PaymentLinkId = paymentLink.PaymentLinkId,
            ExpiresAt = null,
            UserWalletBalanceAfter = null
        };
    }

    public async Task<SnakebiteIncidentPaymentResponse> CreateSnakebiteIncidentWalletPaymentAsync(
        CreateSnakebiteIncidentPaymentRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
        {
            throw new ValidationException("Payment amount must be greater than 0.");
        }

        var incident = await _unitOfWork.GetRepository<SnakebiteIncident>().FirstOrDefaultAsync(
            predicate: i => i.Id == request.SnakebiteIncidentId,
            include: q => q.Include(i => i.Missions).OrderByDescending(i => i.CreatedAt),
            asNoTracking: false,
            cancellationToken: cancellationToken);

        if (incident == null)
        {
            throw new NotFoundException("Snakebite incident was not found.");
        }

        if (incident.UserId != currentUserId)
        {
            throw new ForbiddenException("You are not allowed to pay for this incident.");
        }

        if (incident.Status == SnakebiteIncidentStatus.Completed)
        {
            throw new ConflictException("This incident has already been paid.");
        }

        if (incident.Status != SnakebiteIncidentStatus.Finished)
        {
            throw new ConflictException("Payment is allowed only after the rescue mission is completed.");
        }

        var successfulMission = incident.Missions.FirstOrDefault(m => m.Status == RescueMissionStatus.MissionCompleted);
        if (successfulMission == null)
        {
            throw new ConflictException("No completed mission was found for this incident.");
        }

        var expectedAmount = successfulMission.ActualCost ?? successfulMission.Price;
        if (request.Amount != expectedAmount)
        {
            throw new ValidationException($"Payment amount must equal completed mission amount ({expectedAmount}).");
        }

        var transfer = await MoveMoneyToEscrowAsync(
            currentUserId,
            request.SnakebiteIncidentId,
            request.Amount,
            "Snakebite incident wallet payment",
            "Wallet",
            $"WALLET-{Guid.NewGuid():N}",
            cancellationToken);

        incident.Status = SnakebiteIncidentStatus.Completed;
        _unitOfWork.GetRepository<SnakebiteIncident>().Update(incident);

        await _unitOfWork.CommitAsync();

        return new SnakebiteIncidentPaymentResponse
        {
            SnakebiteIncidentId = request.SnakebiteIncidentId,
            TransactionId = transfer.TransactionId,
            OrderCode = null,
            Amount = request.Amount,
            Currency = "VND",
            Status = "Escrowed",
            Provider = "Wallet",
            CheckoutUrl = null,
            PaymentLinkId = null,
            ExpiresAt = null,
            UserWalletBalanceAfter = transfer.UserWalletBalanceAfter,
            SystemWalletBalanceAfter = transfer.SystemWalletBalanceAfter,
            ExternalTransactionId = transfer.ExternalTransactionId,
            PaidAt = transfer.ProcessedAtUtc
        };
    }

    public async Task<CancelPaymentLinkResponse> CancelSnakebiteIncidentPaymentLinkAsync(
        long orderCode,
        CancelPaymentLinkRequest request,
        CancellationToken cancellationToken)
    {
        var pendingTransaction = await FindIncidentTransactionByOrderCodeAsync(orderCode, false, cancellationToken);

        var gatewayResult = await _paymentGateway.CancelPaymentLinkAsync(orderCode, request.CancellationReason, cancellationToken);

        if (!gatewayResult.Success)
        {
            throw new InvalidOperationException($"Failed to cancel payment link: {gatewayResult.ErrorMessage}");
        }

        if (pendingTransaction != null && string.IsNullOrWhiteSpace(pendingTransaction.ExternalTransactionId))
        {
            _unitOfWork.GetRepository<Transaction>().Delete(pendingTransaction);
            await _unitOfWork.CommitAsync();
        }

        return new CancelPaymentLinkResponse
        {
            Success = true,
            ReferenceId = pendingTransaction?.ReferenceId ?? Guid.Empty,
            OrderCode = gatewayResult.OrderCode,
            Status = PaymentStatus.Cancelled,
            Amount = gatewayResult.Amount,
            AmountPaid = gatewayResult.AmountPaid,
            AmountRemaining = gatewayResult.AmountRemaining,
            Message = "Snakebite incident payment link cancelled successfully"
        };
    }

    public async Task<PayOsWebhookResponse> ProcessSnakebiteIncidentWebhookAsync(
        string rawPayload,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawPayload))
        {
            throw new ArgumentException("Webhook payload cannot be empty", nameof(rawPayload));
        }

        var webhook = _paymentGateway.VerifyWebhook(rawPayload);

        if (!webhook.Success)
        {
            return new PayOsWebhookResponse
            {
                Success = false,
                Message = $"{webhook.Code}: {webhook.Description}",
                OrderCode = webhook.OrderCode,
                Amount = webhook.Amount,
                TransactionReference = webhook.TransactionReference,
                TransactionDateTime = webhook.TransactionDateTime
            };
        }

        return await ProcessConfirmedPayOsPaymentAsync(webhook, cancellationToken);
    }

    public async Task<PayOsWebhookResponse> ConfirmSnakebiteIncidentPaymentAsync(
        Guid transactionId,
        CancellationToken cancellationToken)
    {
        var transaction = await _unitOfWork.GetRepository<Transaction>().FirstOrDefaultAsync(
            predicate: t => t.Id == transactionId,
            asNoTracking: true,
            cancellationToken: cancellationToken);

        if (transaction == null)
        {
            throw new NotFoundException("Transaction not found.");
        }

        // Idempotent: already confirmed
        if (!string.IsNullOrWhiteSpace(transaction.ExternalTransactionId))
        {
            var incident = await _unitOfWork.GetRepository<SnakebiteIncident>().FirstOrDefaultAsync(
                predicate: i => i.Id == transaction.ReferenceId,
                asNoTracking: true,
                cancellationToken: cancellationToken);

            return new PayOsWebhookResponse
            {
                Success = true,
                Message = "Transaction exists and is confirmed.",
                SnakebiteIncidentId = incident?.Id ?? Guid.Empty,
                TransactionId = transaction.Id,
                OrderCode = ExtractOrderCodeFromDescription(transaction.Description),
                Amount = transaction.Amount,
                Status = PaymentStatus.Paid,
                TransactionReference = transaction.ExternalTransactionId,
                TransactionDateTime = transaction.CreatedAt
            };
        }

        // ExternalTransactionId is null → verify on PayOS
        var orderCode = ExtractOrderCodeFromDescription(transaction.Description);
        if (orderCode == 0)
        {
            throw new ConflictException("Incident payment order code is missing.");
        }

        var linkInfo = await _paymentGateway.GetPaymentLinkInformationAsync(orderCode, cancellationToken);
        if (linkInfo == null)
        {
            throw new ConflictException($"Unable to retrieve PayOS payment information for order code {orderCode}.");
        }

        if (!IsPaymentLinkPaid(linkInfo))
        {
            throw new ConflictException($"PayOS reports status '{linkInfo.Status}'. Payment cannot be confirmed.");
        }

        // PayOS says PAID → process confirmed payment
        return await ProcessConfirmedPayOsPaymentAsync(
            new PayOsWebhookData
            {
                Success = true,
                Code = "00",
                Description = "Manual confirmation",
                OrderCode = orderCode,
                Amount = linkInfo.Amount,
                PaymentLinkId = linkInfo.Id,
                TransactionReference = $"INCIDENT-MANUAL-{transactionId:N}",
                TransactionDateTime = DateTime.UtcNow
            },
            cancellationToken);
    }

    public async Task<PayOsWebhookResponse> ConfirmSnakebiteIncidentPaymentByOrderCodeAsync(
        long orderCode,
        CancellationToken cancellationToken)
    {
        var transaction = await FindIncidentTransactionByOrderCodeAsync(orderCode, true, cancellationToken);

        if (transaction == null)
        {
            throw new NotFoundException("Incident transaction not found for given order code.");
        }

        return await ConfirmSnakebiteIncidentPaymentAsync(transaction.Id, cancellationToken);
    }

    public async Task<RefundTransactionResponse> RefundSnakebiteIncidentTransactionAsync(
        RefundTransactionRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
        {
            throw new ValidationException("Refund amount must be greater than 0.");
        }

        var existingTransaction = await _unitOfWork.GetRepository<Transaction>().FirstOrDefaultAsync(
            predicate: t => t.ReferenceId == request.ReferenceId && t.TransactionType == TransactionType.SnakebiteIncidentPayment,
            asNoTracking: true,
            cancellationToken: cancellationToken);

        if (existingTransaction == null)
        {
            throw new NotFoundException("Original payment transaction not found.");
        }

        if (request.Amount > existingTransaction.Amount)
        {
            throw new ValidationException("Refund amount cannot exceed original payment amount.");
        }

        var systemWallet = await GetRequiredWalletAsync(Guid.Parse(SystemWalletUserId), cancellationToken);
        if (systemWallet.Balance < request.Amount)
        {
            throw new ConflictException("System wallet has insufficient balance for refund.");
        }

        var receiverWallet = await GetOrCreateWalletAsync(request.ReceiverId, cancellationToken);

        var systemBefore = systemWallet.Balance;
        var receiverBefore = receiverWallet.Balance;

        systemWallet.Balance -= request.Amount;
        receiverWallet.Balance += request.Amount;

        _unitOfWork.GetRepository<Wallet>().Update(systemWallet);
        _unitOfWork.GetRepository<Wallet>().Update(receiverWallet);

        var refundSourceTx = new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = Guid.Parse(SystemWalletUserId),
            ReferenceId = request.ReferenceId,
            Amount = request.Amount,
            Currency = "VND",
            TransactionType = TransactionType.WalletWithdraw,
            Description = $"Escrow refund source for incident reference {request.ReferenceId}",
            PaymentMethod = "Wallet",
            ExternalTransactionId = $"REFUND-SOURCE-{Guid.NewGuid():N}",
            CreatedAt = DateTime.UtcNow
        };

        var refundTx = new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = request.ReceiverId,
            ReferenceId = request.ReferenceId,
            Amount = request.Amount,
            Currency = "VND",
            TransactionType = TransactionType.SnakebiteIncidentRefund,
            Description = request.Description,
            PaymentMethod = "Wallet",
            ExternalTransactionId = $"REFUND-{Guid.NewGuid():N}",
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.GetRepository<Transaction>().InsertAsync(refundSourceTx);
        await _unitOfWork.GetRepository<Transaction>().InsertAsync(refundTx);

        await _unitOfWork.CommitAsync();

        return new RefundTransactionResponse
        {
            Success = true,
            Message = "Refund successful",
            ReceiverId = request.ReceiverId,
            RefundAmount = request.Amount,
            RefundTransactionId = refundTx.Id,
            SystemWalletBalanceBefore = systemBefore,
            SystemWalletBalanceAfter = systemWallet.Balance,
            ReceiverWalletBalanceBefore = receiverBefore,
            ReceiverWalletBalanceAfter = receiverWallet.Balance,
            RefundedAt = DateTime.UtcNow
        };
    }

    private async Task<Wallet> GetRequiredWalletAsync(Guid userId, CancellationToken cancellationToken)
    {
        var wallet = await _unitOfWork.GetRepository<Wallet>().FirstOrDefaultAsync(
            predicate: w => w.UserId == userId,
            asNoTracking: false,
            cancellationToken: cancellationToken);

        if (wallet == null)
        {
            throw new NotFoundException($"Wallet not found for user {userId}.");
        }

        return wallet;
    }

    private async Task<Wallet> GetOrCreateWalletAsync(Guid userId, CancellationToken cancellationToken)
    {
        var wallet = await _unitOfWork.GetRepository<Wallet>().FirstOrDefaultAsync(
            predicate: w => w.UserId == userId,
            asNoTracking: false,
            cancellationToken: cancellationToken);

        if (wallet != null)
        {
            return wallet;
        }

        wallet = new Wallet
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Balance = 0m
        };

        await _unitOfWork.GetRepository<Wallet>().InsertAsync(wallet);
        return wallet;
    }

    private static long GenerateOrderCode()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var random = new Random().Next(100, 999);
        return long.Parse($"{timestamp}{random}");
    }

    private string BuildDescription(long orderCode, string additionalInfo)
    {
        const int maxLength = 25;
        var baseDescription = $"INCIDENT-{orderCode}".Trim();
        return baseDescription.Length <= maxLength ? baseDescription : baseDescription.Substring(0, maxLength);
    }

    private long ExtractOrderCodeFromDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return 0;
        }

        var parts = description.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0 || !parts[0].StartsWith("INCIDENT-"))
        {
            return 0;
        }

        if (long.TryParse(parts[0].Replace("INCIDENT-", ""), out var orderCode))
        {
            return orderCode;
        }

        return 0;
    }

    private static bool IsPaymentLinkPaid(PayOsLinkInformation linkInfo)
    {
        return linkInfo.Status.Equals("PAID", StringComparison.OrdinalIgnoreCase)
               || (linkInfo.AmountPaid > 0 && linkInfo.AmountPaid >= linkInfo.Amount);
    }

    /// <summary>
    /// Tìm incident transaction theo orderCode (description prefix INCIDENT-{orderCode}).
    /// Mirror pattern từ ConsultationPaymentService.FindConsultationTransactionByOrderCodeAsync.
    /// </summary>
    private async Task<Transaction?> FindIncidentTransactionByOrderCodeAsync(
        long orderCode,
        bool asNoTracking,
        CancellationToken cancellationToken)
    {
        var descriptionPrefix = $"INCIDENT-{orderCode}";
        return await _unitOfWork.GetRepository<Transaction>().FirstOrDefaultAsync(
            predicate: t => t.TransactionType == TransactionType.SnakebiteIncidentPayment
                         && t.Description != null
                         && t.Description.StartsWith(descriptionPrefix)
                         && t.PaymentMethod == "PayOS",
            asNoTracking: asNoTracking,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Tạo pending transaction trước khi gọi PayOS, xóa pending cũ nếu có.
    /// Mirror pattern từ ConsultationPaymentService.PreparePendingPayOsTransactionAsync.
    /// </summary>
    private async Task<PendingPayOsTransactionContext> PreparePendingPayOsTransactionAsync(
        Guid userId,
        Guid incidentId,
        decimal amount,
        string description,
        CancellationToken cancellationToken)
    {
        var existingTransaction = await _unitOfWork.GetRepository<Transaction>().FirstOrDefaultAsync(
            predicate: t => t.ReferenceId == incidentId
                         && t.TransactionType == TransactionType.SnakebiteIncidentPayment
                         && t.PaymentMethod == "PayOS",
            asNoTracking: false,
            cancellationToken: cancellationToken);

        if (existingTransaction != null)
        {
            if (!string.IsNullOrWhiteSpace(existingTransaction.ExternalTransactionId))
            {
                throw new ConflictException("This incident has already been paid.");
            }

            var oldOrderCode = ExtractOrderCodeFromDescription(existingTransaction.Description);
            if (oldOrderCode > 0)
            {
                try
                {
                    await _paymentGateway.CancelPaymentLinkAsync(oldOrderCode, "Replacing old incident payment link.", cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to cancel old PayOS payment link for incident {IncidentId}", incidentId);
                }
            }

            _unitOfWork.GetRepository<Transaction>().Delete(existingTransaction);
            await _unitOfWork.CommitAsync();
        }

        var orderCode = GenerateOrderCode();
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ReferenceId = incidentId,
            Amount = amount,
            Currency = "VND",
            TransactionType = TransactionType.SnakebiteIncidentPayment,
            Description = BuildDescription(orderCode, description),
            PaymentMethod = "PayOS",
            ExternalTransactionId = null,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.GetRepository<Transaction>().InsertAsync(transaction);
        await _unitOfWork.CommitAsync();

        return new PendingPayOsTransactionContext
        {
            TransactionId = transaction.Id,
            OrderCode = orderCode
        };
    }

    /// <summary>
    /// Di chuyển tiền vào escrow (System_Wallet).
    /// Mirror pattern từ ConsultationPaymentService.MoveMoneyToEscrowAsync.
    /// Cho cả Wallet payment (debit user) và PayOS payment (chỉ credit system).
    /// </summary>
    private async Task<(Guid TransactionId, decimal UserWalletBalanceAfter, decimal SystemWalletBalanceAfter, DateTime ProcessedAtUtc, string ExternalTransactionId)> MoveMoneyToEscrowAsync(
        Guid userId,
        Guid incidentId,
        decimal amount,
        string description,
        string paymentMethod,
        string externalTransactionId,
        CancellationToken cancellationToken,
        bool skipExistingPaymentInsert = false)
    {
        decimal userWalletBalanceAfter;
        var systemWallet = await GetOrCreateWalletAsync(Guid.Parse(SystemWalletUserId), cancellationToken);
        var now = DateTime.UtcNow;

        if (string.Equals(paymentMethod, "Wallet", StringComparison.OrdinalIgnoreCase))
        {
            var userWallet = await GetRequiredWalletAsync(userId, cancellationToken);
            if (userWallet.Balance < amount)
            {
                throw new ConflictException($"Insufficient wallet balance. Available: {userWallet.Balance}, required: {amount}.");
            }

            userWallet.Balance -= amount;
            userWalletBalanceAfter = userWallet.Balance;
            _unitOfWork.GetRepository<Wallet>().Update(userWallet);
        }
        else
        {
            var userWallet = await _unitOfWork.GetRepository<Wallet>().FirstOrDefaultAsync(
                predicate: w => w.UserId == userId,
                asNoTracking: false,
                cancellationToken: cancellationToken);
            userWalletBalanceAfter = userWallet?.Balance ?? 0m;
        }

        systemWallet.Balance += amount;
        _unitOfWork.GetRepository<Wallet>().Update(systemWallet);

        Transaction paymentTx;
        if (skipExistingPaymentInsert)
        {
            paymentTx = await _unitOfWork.GetRepository<Transaction>().FirstOrDefaultAsync(
                predicate: t => t.ReferenceId == incidentId && t.TransactionType == TransactionType.SnakebiteIncidentPayment,
                asNoTracking: false,
                cancellationToken: cancellationToken)
                ?? throw new ConflictException("Incident payment transaction was not found.");
        }
        else
        {
            paymentTx = new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ReferenceId = incidentId,
                Amount = amount,
                Currency = "VND",
                TransactionType = TransactionType.SnakebiteIncidentPayment,
                Description = description,
                PaymentMethod = paymentMethod,
                ExternalTransactionId = externalTransactionId,
                CreatedAt = now
            };

            await _unitOfWork.GetRepository<Transaction>().InsertAsync(paymentTx);
        }

        var systemCreditTx = new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = Guid.Parse(SystemWalletUserId),
            ReferenceId = incidentId,
            Amount = amount,
            Currency = "VND",
            TransactionType = TransactionType.WalletTopup,
            Description = $"Escrow received for incident reference {incidentId}",
            PaymentMethod = paymentMethod,
            ExternalTransactionId = externalTransactionId,
            CreatedAt = now
        };

        await _unitOfWork.GetRepository<Transaction>().InsertAsync(systemCreditTx);

        return (paymentTx.Id, userWalletBalanceAfter, systemWallet.Balance, now, externalTransactionId);
    }

    /// <summary>
    /// Xử lý PayOS payment đã confirmed (từ webhook hoặc manual confirm).
    /// Mirror pattern từ ConsultationPaymentService.ProcessConfirmedPayOsPaymentAsync.
    /// Simplified: không có booking/ping logic, chỉ update incident status.
    /// </summary>
    private async Task<PayOsWebhookResponse> ProcessConfirmedPayOsPaymentAsync(
        PayOsWebhookData webhook,
        CancellationToken cancellationToken)
    {
        var transaction = await FindIncidentTransactionByOrderCodeAsync(webhook.OrderCode, false, cancellationToken);
        if (transaction == null)
        {
            throw new InvalidOperationException($"Incident payment transaction with orderCode {webhook.OrderCode} was not found.");
        }

        // Idempotent check: if ExternalTransactionId already set, return existing result
        if (!string.IsNullOrWhiteSpace(transaction.ExternalTransactionId))
        {
            var incident = await _unitOfWork.GetRepository<SnakebiteIncident>().FirstOrDefaultAsync(
                predicate: i => i.Id == transaction.ReferenceId,
                asNoTracking: true,
                cancellationToken: cancellationToken);

            return new PayOsWebhookResponse
            {
                Success = true,
                Message = "Payment already processed",
                SnakebiteIncidentId = incident?.Id ?? Guid.Empty,
                TransactionId = transaction.Id,
                OrderCode = webhook.OrderCode,
                Amount = transaction.Amount,
                Status = PaymentStatus.Paid,
                TransactionReference = transaction.ExternalTransactionId,
                TransactionDateTime = transaction.CreatedAt
            };
        }

        // Set ExternalTransactionId on the pending transaction
        transaction.ExternalTransactionId = string.IsNullOrWhiteSpace(webhook.TransactionReference)
            ? $"INCIDENT-WEBHOOK-{transaction.Id:N}"
            : webhook.TransactionReference;
        transaction.CreatedAt = webhook.TransactionDateTime ?? DateTime.UtcNow;
        _unitOfWork.GetRepository<Transaction>().Update(transaction);

        // Move money to escrow (credit System_Wallet + create WalletTopup)
        var escrowTransfer = await MoveMoneyToEscrowAsync(
            transaction.UserId,
            transaction.ReferenceId,
            transaction.Amount,
            "Incident payment via PayOS",
            "PayOS",
            transaction.ExternalTransactionId,
            cancellationToken,
            skipExistingPaymentInsert: true);

        // Update incident status to Completed
        var incidentToUpdate = await _unitOfWork.GetRepository<SnakebiteIncident>().FirstOrDefaultAsync(
            predicate: i => i.Id == transaction.ReferenceId,
            asNoTracking: false,
            cancellationToken: cancellationToken);

        if (incidentToUpdate != null)
        {
            incidentToUpdate.Status = SnakebiteIncidentStatus.Completed;
            _unitOfWork.GetRepository<SnakebiteIncident>().Update(incidentToUpdate);
        }

        await _unitOfWork.CommitAsync();

        return new PayOsWebhookResponse
        {
            Success = true,
            Message = "Payment processed successfully",
            SnakebiteIncidentId = transaction.ReferenceId,
            TransactionId = transaction.Id,
            OrderCode = webhook.OrderCode,
            Amount = transaction.Amount,
            Status = PaymentStatus.Paid,
            TransactionReference = transaction.ExternalTransactionId,
            TransactionDateTime = transaction.CreatedAt
        };
    }

    private sealed class PendingPayOsTransactionContext
    {
        public Guid TransactionId { get; init; }
        public long OrderCode { get; init; }
    }
}