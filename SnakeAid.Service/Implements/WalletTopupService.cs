using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Messages.Notifications;
using SnakeAid.Core.Requests.Wallet;
using SnakeAid.Core.Responses.PayOs;
using SnakeAid.Core.Responses.Wallet;
using SnakeAid.Core.Settings;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;
using SnakeAid.Service.Services.PayOs;
using SnakeAid.Service.Services.PayOs.Models;

namespace SnakeAid.Service.Implements;

public class WalletTopupService : IWalletTopupService
{
    private readonly IPaymentGateway _paymentGateway;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<WalletTopupService> _logger;
    private readonly INotificationQueueService? _notificationQueueService;
    private const string DefaultItemName = "Wallet Top-up";
    private const string LogPrefix = "[WalletTopup]";
    private static readonly Regex OrderCodeRegex = new($@"^{PayOsPaymentFlowPrefixes.Topup}(\d+)", RegexOptions.Compiled);

    public WalletTopupService(
        IPaymentGateway paymentGateway,
        IUnitOfWork unitOfWork,
        IOptions<PayOsOptions> options,
        ILogger<WalletTopupService> logger,
        INotificationQueueService? notificationQueueService = null)
    {
        _paymentGateway = paymentGateway;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _notificationQueueService = notificationQueueService;
    }

    public async Task<CreateWalletTopupResponse> CreateWalletTopupAsync(
        CreateWalletTopupRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("{Prefix} Creating wallet top-up for UserId {UserId}, Amount {Amount}",
                LogPrefix, currentUserId, request.Amount);

            if (request.Amount <= 0)
            {
                throw new InvalidOperationException("Top-up amount must be greater than 0");
            }

            var userWallet = await _unitOfWork.GetRepository<Wallet>()
                .FirstOrDefaultAsync(
                    predicate: w => w.UserId == currentUserId,
                    asNoTracking: false,
                    cancellationToken: cancellationToken);

            if (userWallet == null)
            {
                throw new InvalidOperationException($"User wallet not found for user {currentUserId}");
            }

            var existingTransaction = await _unitOfWork.GetRepository<Transaction>()
                .FirstOrDefaultAsync(
                    predicate: t => t.UserId == currentUserId &&
                                   t.TransactionType == TransactionType.WalletTopup &&
                                   string.IsNullOrEmpty(t.ExternalTransactionId),
                    asNoTracking: false,
                    cancellationToken: cancellationToken);

            if (existingTransaction != null)
            {
                throw new InvalidOperationException(
                    "You have a pending wallet top-up transaction. Please complete or cancel it before creating a new one.");
            }

            var orderCode = GenerateOrderCode();
            var description = BuildDescription(orderCode, request.Description ?? "Wallet top-up");

            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = currentUserId,
                ReferenceId = currentUserId,
                Amount = request.Amount,
                Currency = "VND",
                TransactionType = TransactionType.WalletTopup,
                Description = description,
                PaymentMethod = "PayOS",
                ExternalTransactionId = null,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.GetRepository<Transaction>().InsertAsync(transaction);
            await _unitOfWork.CommitAsync();

            var payOsResult = await _paymentGateway.CreatePaymentLinkAsync(
                new PayOsCreatePaymentRequest
                {
                    OrderCode = orderCode,
                    Amount = request.Amount,
                    Description = description,
                    ItemName = DefaultItemName,
                    Quantity = 1
                },
                cancellationToken);

            if (!payOsResult.Success)
            {
                _logger.LogWarning("{Prefix} PayOS payment link creation failed, cleaning up transaction {TransactionId}. Error: {Error}",
                    LogPrefix, transaction.Id, payOsResult.ErrorMessage);

                _unitOfWork.GetRepository<Transaction>().Delete(transaction);
                await _unitOfWork.CommitAsync();

                throw new InvalidOperationException($"Failed to create PayOS payment link: {payOsResult.ErrorMessage}");
            }

            _logger.LogInformation("{Prefix} Wallet top-up payment link created. TransactionId={TransactionId}, OrderCode={OrderCode}, CheckoutUrl={CheckoutUrl}",
                LogPrefix, transaction.Id, orderCode, payOsResult.CheckoutUrl);

            return new CreateWalletTopupResponse
            {
                TransactionId = transaction.Id,
                UserId = currentUserId,
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
            _logger.LogError(ex, "{Prefix} Failed to create wallet top-up for UserId {UserId}",
                LogPrefix, currentUserId);
            throw;
        }
    }

    public async Task<PayOsWebhookResponse> ProcessWalletTopupWebhookAsync(
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

        return await ProcessConfirmedPaymentAsync(webhook, cancellationToken);
    }

    public async Task<PayOsWebhookResponse> ConfirmWalletTopupAsync(
        Guid transactionId,
        CancellationToken cancellationToken)
    {
        var transaction = await FindTransactionByIdAsync(transactionId, true, cancellationToken);
        if (transaction == null)
        {
            throw new InvalidOperationException($"Transaction {transactionId} not found");
        }

        if (!string.IsNullOrWhiteSpace(transaction.ExternalTransactionId))
        {
            return BuildSuccessResponse(transaction, ExtractOrderCodeFromDescription(transaction.Description));
        }

        var orderCode = ExtractOrderCodeFromDescription(transaction.Description);
        if (orderCode == 0)
        {
            throw new InvalidOperationException($"Cannot extract orderCode from transaction description: {transaction.Description}");
        }

        var linkInfo = await _paymentGateway.GetPaymentLinkInformationAsync(orderCode, cancellationToken);
        if (linkInfo == null)
        {
            throw new InvalidOperationException($"Unable to retrieve payment information for orderCode {orderCode}");
        }

        if (!IsPaymentLinkPaid(linkInfo))
        {
            throw new InvalidOperationException($"PayOS reports status '{linkInfo.Status}'. Payment cannot be confirmed.");
        }

        return await ProcessConfirmedPaymentAsync(
            new PayOsWebhookData
            {
                Success = true,
                Code = "00",
                Description = "Manual confirmation",
                OrderCode = orderCode,
                Amount = linkInfo.Amount,
                PaymentLinkId = linkInfo.Id,
                TransactionReference = $"{PayOsPaymentFlowPrefixes.Topup}MANUAL-{transactionId:N}",
                TransactionDateTime = DateTime.UtcNow
            },
            cancellationToken);
    }

    public async Task<PayOsWebhookResponse> ConfirmWalletTopupByOrderCodeAsync(
        long orderCode,
        CancellationToken cancellationToken)
    {
        var transaction = await FindTransactionByOrderCodeAsync(orderCode, true, cancellationToken);
        if (transaction == null)
        {
            throw new InvalidOperationException($"Transaction with orderCode {orderCode} not found");
        }

        return await ConfirmWalletTopupAsync(transaction.Id, cancellationToken);
    }

    private async Task<PayOsWebhookResponse> ProcessConfirmedPaymentAsync(
        PayOsWebhookData webhook,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("{Prefix} Processing confirmed wallet top-up. OrderCode={OrderCode}, TransactionRef={TransactionRef}",
            LogPrefix, webhook.OrderCode, webhook.TransactionReference);

        var shouldNotify = false;
        var userId = Guid.Empty;
        var transactionAmount = 0m;
        var response = await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var transaction = await FindTransactionByOrderCodeAsync(webhook.OrderCode, true, cancellationToken);
            if (transaction == null)
            {
                throw new InvalidOperationException($"Wallet top-up transaction with orderCode {webhook.OrderCode} not found");
            }

            if (!string.IsNullOrWhiteSpace(transaction.ExternalTransactionId))
            {
                return BuildSuccessResponse(transaction, webhook.OrderCode);
            }

            var externalTransactionId = string.IsNullOrWhiteSpace(webhook.TransactionReference)
                ? $"{PayOsPaymentFlowPrefixes.Topup}WEBHOOK-{transaction.Id:N}"
                : webhook.TransactionReference;
            var confirmedAt = webhook.TransactionDateTime ?? DateTime.UtcNow;

            var claimSucceeded = await TryMarkTransactionConfirmedAsync(
                transaction.Id,
                externalTransactionId,
                confirmedAt,
                cancellationToken);

            if (!claimSucceeded)
            {
                var confirmedTransaction = await FindTransactionByIdAsync(transaction.Id, true, cancellationToken)
                    ?? throw new InvalidOperationException($"Transaction {transaction.Id} disappeared during top-up confirmation.");
                return BuildSuccessResponse(confirmedTransaction, webhook.OrderCode);
            }

            userId = transaction.UserId
                ?? throw new InvalidOperationException("Wallet top-up transaction is missing user ownership.");
            transactionAmount = transaction.Amount;
            shouldNotify = true;

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
                _logger.LogInformation("{Prefix} Created wallet for user {UserId}", LogPrefix, userId);
            }

            var previousBalance = userWallet.Balance;
            userWallet.Balance += transaction.Amount;
            _unitOfWork.GetRepository<Wallet>().Update(userWallet);

            transaction.ExternalTransactionId = externalTransactionId;
            transaction.CreatedAt = confirmedAt;

            _logger.LogInformation("{Prefix} Wallet top-up completed. UserId={UserId}, Amount={Amount}, Balance: {PreviousBalance} -> {NewBalance}",
                LogPrefix, userId, transaction.Amount, previousBalance, userWallet.Balance);

            return BuildSuccessResponse(transaction, webhook.OrderCode);
        });

        if (shouldNotify && _notificationQueueService != null && userId != Guid.Empty)
        {
            await _notificationQueueService.PublishAsync(new NotificationMessage
            {
                UserId = userId,
                Title = "Nạp ví thành công",
                Body = $"Bạn đã nạp thành công {FormatVnd(transactionAmount)} vào ví qua PayOS.",
                Type = "WALLET_TOPUP_SUCCESS",
                Data = new Dictionary<string, string>
                {
                    ["orderCode"] = response.OrderCode.ToString(),
                    ["transactionId"] = response.TransactionId.ToString(),
                    ["paymentMethod"] = "PayOS"
                }
            }, cancellationToken);
        }

        return response;
    }

    private async Task<bool> TryMarkTransactionConfirmedAsync(
        Guid transactionId,
        string externalTransactionId,
        DateTime confirmedAt,
        CancellationToken cancellationToken)
    {
        var dbContext = GetDbContext();

        var affectedRows = await dbContext.Set<Transaction>()
            .Where(t => t.Id == transactionId && t.ExternalTransactionId == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(t => t.ExternalTransactionId, externalTransactionId)
                    .SetProperty(t => t.CreatedAt, confirmedAt),
                cancellationToken);

        if (affectedRows == 1)
        {
            var trackedEntry = dbContext.ChangeTracker.Entries<Transaction>()
                .FirstOrDefault(e => e.Entity.Id == transactionId);
            if (trackedEntry != null)
            {
                trackedEntry.Entity.ExternalTransactionId = externalTransactionId;
                trackedEntry.Entity.CreatedAt = confirmedAt;
            }
        }

        return affectedRows == 1;
    }

    private async Task<Transaction?> FindTransactionByIdAsync(
        Guid transactionId,
        bool asNoTracking,
        CancellationToken cancellationToken)
    {
        return await _unitOfWork.GetRepository<Transaction>()
            .FirstOrDefaultAsync(
                predicate: t => t.Id == transactionId && t.TransactionType == TransactionType.WalletTopup,
                asNoTracking: asNoTracking,
                cancellationToken: cancellationToken);
    }

    private async Task<Transaction?> FindTransactionByOrderCodeAsync(
        long orderCode,
        bool asNoTracking,
        CancellationToken cancellationToken)
    {
        var descriptionPrefix = PayOsPaymentFlowPrefixes.BuildOrderCodePrefix(PayOsPaymentFlow.Topup, orderCode);
        return await _unitOfWork.GetRepository<Transaction>()
            .FirstOrDefaultAsync(
                predicate: t => t.TransactionType == TransactionType.WalletTopup &&
                               t.Description != null &&
                               t.Description.StartsWith(descriptionPrefix),
                asNoTracking: asNoTracking,
                cancellationToken: cancellationToken);
    }

    private static PayOsWebhookResponse BuildSuccessResponse(Transaction transaction, long orderCode)
    {
        return new PayOsWebhookResponse
        {
            Success = true,
            Message = "Wallet top-up confirmed successfully",
            TransactionId = transaction.Id,
            OrderCode = orderCode,
            Amount = transaction.Amount,
            Status = PaymentStatus.Paid,
            TransactionReference = transaction.ExternalTransactionId ?? string.Empty,
            TransactionDateTime = transaction.CreatedAt
        };
    }

    private SnakeAidDbContext GetDbContext()
    {
        if (_unitOfWork is IUnitOfWork<SnakeAidDbContext> dbUnitOfWork)
        {
            return dbUnitOfWork.Context;
        }

        throw new InvalidOperationException("WalletTopupService requires IUnitOfWork<SnakeAidDbContext> for atomic confirmation updates.");
    }

    private static string FormatVnd(decimal amount)
    {
        return amount.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + " ₫";
    }

    private static long GenerateOrderCode()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var randomPart = RandomNumberGenerator.GetInt32(1000, 9999);
        return long.Parse($"{timestamp}{randomPart}");
    }

    private string BuildDescription(long orderCode, string? customDescription)
    {
        const int maxLength = 25;
        var baseDescription = PayOsPaymentFlowPrefixes.BuildOrderCodePrefix(PayOsPaymentFlow.Topup, orderCode);
        if (baseDescription.Length >= maxLength || string.IsNullOrWhiteSpace(customDescription))
        {
            return baseDescription.Length > maxLength ? baseDescription[..maxLength] : baseDescription;
        }

        var remaining = maxLength - baseDescription.Length - 1;
        if (remaining <= 0)
        {
            return baseDescription[..maxLength];
        }

        var suffix = customDescription.Length > remaining ? customDescription[..remaining] : customDescription;
        return $"{baseDescription}-{suffix}";
    }

    private long ExtractOrderCodeFromDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return 0;
        }

        var match = OrderCodeRegex.Match(description);
        return match.Success && long.TryParse(match.Groups[1].Value, out var orderCode)
            ? orderCode
            : 0;
    }

    private static bool IsPaymentLinkPaid(PayOsLinkInformation linkInfo)
    {
        return linkInfo.Status.Equals("PAID", StringComparison.OrdinalIgnoreCase) ||
               (linkInfo.AmountPaid > 0 && linkInfo.AmountPaid >= linkInfo.Amount);
    }
}
