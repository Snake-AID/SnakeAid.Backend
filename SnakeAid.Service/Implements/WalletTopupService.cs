using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Requests.Wallet;
using SnakeAid.Core.Responses.Wallet;
using SnakeAid.Core.Settings;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;
using SnakeAid.Service.Services.PayOs.Models;

namespace SnakeAid.Service.Implements;

public class WalletTopupService : IWalletTopupService
{
    private readonly IPaymentGateway _paymentGateway;
    private readonly IUnitOfWork _unitOfWork;
    private readonly PayOsOptions _options;
    private readonly ILogger<WalletTopupService> _logger;
    private const string DefaultItemName = "Wallet Top-up";
    private const string LogPrefix = "[WalletTopup]";
    private static readonly Regex OrderCodeRegex = new(@"^SNAKEAID-(\d+)", RegexOptions.Compiled);

    public WalletTopupService(
        IPaymentGateway paymentGateway,
        IUnitOfWork unitOfWork,
        IOptions<PayOsOptions> options,
        ILogger<WalletTopupService> logger)
    {
        _paymentGateway = paymentGateway;
        _unitOfWork = unitOfWork;
        _options = options.Value;
        _logger = logger;
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

            // Validate amount
            if (request.Amount <= 0)
            {
                throw new InvalidOperationException("Top-up amount must be greater than 0");
            }

            // Check if user wallet exists
            var userWallet = await _unitOfWork.GetRepository<Wallet>()
                .FirstOrDefaultAsync(
                    predicate: w => w.UserId == currentUserId,
                    asNoTracking: false,
                    cancellationToken: cancellationToken);

            if (userWallet == null)
            {
                throw new InvalidOperationException($"User wallet not found for user {currentUserId}");
            }

            // Check if there's already a pending top-up transaction
            var existingTransaction = await _unitOfWork.GetRepository<Transaction>()
                .FirstOrDefaultAsync(
                    predicate: t => t.UserId == currentUserId &&
                                   t.TransactionType == TransactionType.WalletTopup &&
                                   string.IsNullOrEmpty(t.ExternalTransactionId), // No external ID means not completed
                    asNoTracking: false,
                    cancellationToken: cancellationToken);

            if (existingTransaction != null)
            {
                throw new InvalidOperationException(
                    $"You have a pending wallet top-up transaction. Please complete or cancel it before creating a new one.");
            }

            // Generate orderCode and description
            var orderCode = GenerateOrderCode();
            var description = BuildDescription(orderCode, request.Description ?? "Wallet top-up");

            // Create Transaction record (Pending)
            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = currentUserId,
                ReferenceId = currentUserId, // For wallet top-up, reference is the user themselves
                Amount = request.Amount,
                Currency = "VND",
                TransactionType = TransactionType.WalletTopup,
                Description = description,
                PaymentMethod = "PayOS",
                ExternalTransactionId = null, // Will be updated on webhook
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.GetRepository<Transaction>().InsertAsync(transaction);
            await _unitOfWork.CommitAsync();

            // Create PayOS payment link after ensuring DB record exists
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
                // Payment link creation failed - clean up the transaction record
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
                ExpiresAt = null, // PayOS handles expiration
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

    private static long GenerateOrderCode()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        // Use Guid for better uniqueness guarantee
        var guid = Guid.NewGuid();
        var hash = guid.GetHashCode() & 0x7FFFFFFF; // Ensure positive
        var randomPart = hash % 9000 + 1000; // 4-digit number: 1000-9999
        return long.Parse($"{timestamp:D13}{randomPart:D4}");
    }

    private string BuildDescription(long orderCode, string? customDescription)
    {
        var baseDescription = $"SNAKEAID-{orderCode}";
        return string.IsNullOrEmpty(customDescription)
            ? baseDescription
            : $"{baseDescription} - {customDescription}";
    }
}
