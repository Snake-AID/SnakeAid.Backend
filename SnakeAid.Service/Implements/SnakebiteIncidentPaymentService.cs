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

        var orderCode = GenerateOrderCode();
        var description = BuildDescription(orderCode, request.Description ?? string.Empty);

        var paymentLink = await _paymentGateway.CreatePaymentLinkAsync(new PayOsCreatePaymentRequest
        {
            OrderCode = orderCode,
            Amount = request.Amount,
            Description = description,
            ItemName = "Snakebite Incident Payment",
            Quantity = 1
        }, cancellationToken);

        if (!paymentLink.Success)
        {
            throw new ValidationException("Payment link creation failed: " + (paymentLink.ErrorMessage ?? "Unknown error"));
        }

        incident.PayOsOrderCode = orderCode;
        _unitOfWork.GetRepository<SnakebiteIncident>().Update(incident);
        await _unitOfWork.CommitAsync();

        return new SnakebiteIncidentPaymentResponse
        {
            SnakebiteIncidentId = request.SnakebiteIncidentId,
            TransactionId = null,
            OrderCode = paymentLink.OrderCode,
            Amount = request.Amount,
            Currency = "VND",
            Status = PaymentStatus.Pending,
            Provider = "PayOS",
            CheckoutUrl = paymentLink.CheckoutUrl,
            PaymentLinkId = paymentLink.PaymentLinkId,  // Still return to client, just don't store in DB
            ExpiresAt = null
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

        var userWallet = await GetRequiredWalletAsync(currentUserId, cancellationToken);
        if (userWallet.Balance < request.Amount)
        {
            throw new ConflictException($"Insufficient wallet balance. Available: {userWallet.Balance}, required: {request.Amount}.");
        }

        var systemWallet = await GetOrCreateWalletAsync(Guid.Parse(SystemWalletUserId), cancellationToken);

        userWallet.Balance -= request.Amount;
        systemWallet.Balance += request.Amount;

        _unitOfWork.GetRepository<Wallet>().Update(userWallet);
        _unitOfWork.GetRepository<Wallet>().Update(systemWallet);

        var paymentTransaction = new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = currentUserId,
            ReferenceId = request.SnakebiteIncidentId,
            Amount = request.Amount,
            Currency = "VND",
            TransactionType = TransactionType.SnakebiteIncidentPayment,
            Description = $"Snakebite incident wallet payment: {request.SnakebiteIncidentId}",
            PaymentMethod = "Wallet",
            ExternalTransactionId = $"WALLET-{Guid.NewGuid():N}",
            CreatedAt = DateTime.UtcNow
        };

        var systemCreditTransaction = new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = Guid.Parse(SystemWalletUserId),
            ReferenceId = request.SnakebiteIncidentId,
            Amount = request.Amount,
            Currency = "VND",
            TransactionType = TransactionType.WalletTopup,
            Description = $"Escrowed wallet payment for snakebite incident {request.SnakebiteIncidentId}",
            PaymentMethod = "Wallet",
            ExternalTransactionId = $"WALLET-{Guid.NewGuid():N}",
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.GetRepository<Transaction>().InsertAsync(paymentTransaction);
        await _unitOfWork.GetRepository<Transaction>().InsertAsync(systemCreditTransaction);

        incident.Status = SnakebiteIncidentStatus.Completed;
        _unitOfWork.GetRepository<SnakebiteIncident>().Update(incident);

        await _unitOfWork.CommitAsync();

        return new SnakebiteIncidentPaymentResponse
        {
            SnakebiteIncidentId = request.SnakebiteIncidentId,
            TransactionId = paymentTransaction.Id,
            OrderCode = null,
            Amount = request.Amount,
            Currency = "VND",
            Status = PaymentStatus.Paid,
            Provider = "Wallet",
            CheckoutUrl = null,
            PaymentLinkId = null,
            ExpiresAt = null,
            ExternalTransactionId = paymentTransaction.ExternalTransactionId,
            PaidAt = paymentTransaction.CreatedAt
        };
    }

    public async Task<CancelPaymentLinkResponse> CancelSnakebiteIncidentPaymentLinkAsync(
        long orderCode,
        CancelPaymentLinkRequest request,
        CancellationToken cancellationToken)
    {
        var gatewayResult = await _paymentGateway.CancelPaymentLinkAsync(orderCode, request.CancellationReason, cancellationToken);

        if (!gatewayResult.Success)
        {
            throw new InvalidOperationException($"Failed to cancel payment link: {gatewayResult.ErrorMessage}");
        }

        var paymentTransaction = await _unitOfWork.GetRepository<Transaction>().FirstOrDefaultAsync(
            predicate: t => !string.IsNullOrWhiteSpace(t.Description) && t.Description.StartsWith($"INCIDENT-{orderCode}"),
            asNoTracking: false,
            cancellationToken: cancellationToken);

        if (paymentTransaction != null)
        {
            _unitOfWork.GetRepository<Transaction>().Delete(paymentTransaction);
            await _unitOfWork.CommitAsync();
        }

        var incident = await _unitOfWork.GetRepository<SnakebiteIncident>().FirstOrDefaultAsync(
            predicate: i => i.PayOsOrderCode == orderCode,
            asNoTracking: true,
            cancellationToken: cancellationToken);

        return new CancelPaymentLinkResponse
        {
            Success = true,
            ReferenceId = incident?.Id ?? Guid.Empty,
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

        var incident = await _unitOfWork.GetRepository<SnakebiteIncident>().FirstOrDefaultAsync(
            predicate: i => i.PayOsOrderCode == webhook.OrderCode,
            asNoTracking: false,
            cancellationToken: cancellationToken);

        if (incident == null)
        {
            throw new NotFoundException("Snakebite incident not found for webhook orderCode.");
        }

        Transaction? confirmedTransaction = null;

        if (webhook.Success)
        {
            confirmedTransaction = await _unitOfWork.GetRepository<Transaction>().FirstOrDefaultAsync(
                predicate: t => t.ReferenceId == incident.Id && t.TransactionType == TransactionType.SnakebiteIncidentPayment,
                asNoTracking: true,
                cancellationToken: cancellationToken);

            if (confirmedTransaction == null)
            {
                confirmedTransaction = new Transaction
                {
                    Id = Guid.NewGuid(),
                    UserId = incident.UserId,
                    ReferenceId = incident.Id,
                    Amount = webhook.Amount,
                    Currency = "VND",
                    TransactionType = TransactionType.SnakebiteIncidentPayment,
                    Description = $"INCIDENT-{webhook.OrderCode}",
                    PaymentMethod = "PayOS",
                    ExternalTransactionId = webhook.TransactionReference,
                    CreatedAt = webhook.TransactionDateTime ?? DateTime.UtcNow
                };

                await _unitOfWork.GetRepository<Transaction>().InsertAsync(confirmedTransaction);
            }
            else
            {
                confirmedTransaction.ExternalTransactionId = webhook.TransactionReference;
                _unitOfWork.GetRepository<Transaction>().Update(confirmedTransaction);
            }

            incident.Status = SnakebiteIncidentStatus.Completed;
            _unitOfWork.GetRepository<SnakebiteIncident>().Update(incident);

            await _unitOfWork.CommitAsync();
        }

        return new PayOsWebhookResponse
        {
            Success = webhook.Success,
            Message = webhook.Success ? "Payment processed successfully" : "Payment failed",
            SnakebiteIncidentId = incident.Id,
            TransactionId = confirmedTransaction?.Id,
            OrderCode = webhook.OrderCode,
            Amount = webhook.Amount,
            Status = webhook.Success ? PaymentStatus.Paid : PaymentStatus.Failed,
            TransactionReference = webhook.TransactionReference,
            TransactionDateTime = webhook.TransactionDateTime
        };
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
            TransactionReference = transaction.ExternalTransactionId ?? string.Empty,
            TransactionDateTime = transaction.CreatedAt
        };
    }

    public async Task<PayOsWebhookResponse> ConfirmSnakebiteIncidentPaymentByOrderCodeAsync(
        long orderCode,
        CancellationToken cancellationToken)
    {
        var incident = await _unitOfWork.GetRepository<SnakebiteIncident>().FirstOrDefaultAsync(
            predicate: i => i.PayOsOrderCode == orderCode,
            asNoTracking: true,
            cancellationToken: cancellationToken);

        if (incident == null)
        {
            throw new NotFoundException("Snakebite incident not found for given order code.");
        }

        var transaction = await _unitOfWork.GetRepository<Transaction>().FirstOrDefaultAsync(
            predicate: t => t.ReferenceId == incident.Id && t.TransactionType == TransactionType.SnakebiteIncidentPayment,
            asNoTracking: true,
            cancellationToken: cancellationToken);

        if (transaction == null)
        {
            throw new NotFoundException("Transaction not found for given order code.");
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
            Description = request.Description,
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
}
