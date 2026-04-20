using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Enums;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Messages.Notifications;
using SnakeAid.Core.Requests.Consultation;
using SnakeAid.Core.Responses.Consultation;
using SnakeAid.Core.Responses.PayOs;
using SnakeAid.Core.Constants;
using SnakeAid.Core.Services;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;
using SnakeAid.Service.Services.PayOs;
using SnakeAid.Service.Services.PayOs.Models;

namespace SnakeAid.Service.Implements;

public class ConsultationPaymentService : IConsultationPaymentService
{
    private static readonly TimeSpan EmergencyRequestTtl = TimeSpan.FromMinutes(2);
    private static readonly Regex OrderCodeRegex = new($@"^{PayOsPaymentFlowPrefixes.Consultation}(\d+)", RegexOptions.Compiled);
    private const decimal DefaultConsultationPlatformFeePercent = 0.20m;

    private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
    private readonly IExpertEmergencyNotificationService _notificationService;
    private readonly INotificationQueueService? _notificationQueueService;
    private readonly IPaymentGateway _paymentGateway;
    private readonly ISystemSettingService? _systemSettingService;
    private readonly ILogger<ConsultationPaymentService> _logger;

    public ConsultationPaymentService(
        IUnitOfWork<SnakeAidDbContext> unitOfWork,
        IExpertEmergencyNotificationService notificationService,
        IPaymentGateway paymentGateway,
        ILogger<ConsultationPaymentService> logger,
        INotificationQueueService? notificationQueueService = null,
        ISystemSettingService? systemSettingService = null)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _paymentGateway = paymentGateway;
        _logger = logger;
        _notificationQueueService = notificationQueueService;
        _systemSettingService = systemSettingService;
    }

    public async Task<ConsultationPaymentResponse> PayScheduledBookingAsync(
        Guid userId,
        Guid bookingId,
        ProcessConsultationPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        return request.PaymentMethod switch
        {
            ConsultationPaymentMethod.WalletBalance => await PayScheduledBookingWithWalletAsync(userId, bookingId, request, cancellationToken),
            ConsultationPaymentMethod.PayOs => await CreateScheduledBookingPayOsIntentAsync(userId, bookingId, request, cancellationToken),
            _ => throw new ValidationException($"Unsupported payment method: {request.PaymentMethod}.")
        };
    }

    public async Task<ConsultationPaymentResponse> PayEmergencyRequestAsync(
        Guid userId,
        Guid requestId,
        ProcessConsultationPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        return request.PaymentMethod switch
        {
            ConsultationPaymentMethod.WalletBalance => await PayEmergencyRequestWithWalletAsync(userId, requestId, request, cancellationToken),
            ConsultationPaymentMethod.PayOs => await CreateEmergencyRequestPayOsIntentAsync(userId, requestId, request, cancellationToken),
            _ => throw new ValidationException($"Unsupported payment method: {request.PaymentMethod}.")
        };
    }

    public async Task<ConsultationPaymentResponse> ConfirmConsultationPaymentAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default)
    {
        var transaction = await _unitOfWork.GetRepository<Transaction>().FirstOrDefaultAsync(
            predicate: t => t.Id == transactionId,
            asNoTracking: true,
            cancellationToken: cancellationToken);

        if (transaction == null)
        {
            throw new NotFoundException("Consultation payment transaction was not found.");
        }

        EnsureConsultationPayOsTransaction(transaction);

        if (!string.IsNullOrWhiteSpace(transaction.ExternalTransactionId))
        {
            return await BuildConfirmedResponseAsync(transaction, cancellationToken);
        }

        var orderCode = ExtractOrderCodeFromDescription(transaction.Description);
        if (orderCode == 0)
        {
            throw new ConflictException("Consultation payment order code is missing.");
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

        var result = await ProcessConfirmedPayOsPaymentAsync(
            new PayOsWebhookData
            {
                Success = true,
                Code = "00",
                Description = "Manual confirmation",
                OrderCode = orderCode,
                Amount = linkInfo.Amount,
                PaymentLinkId = linkInfo.Id,
                TransactionReference = $"CONSULT-MANUAL-{transactionId:N}",
                TransactionDateTime = DateTime.UtcNow
            },
            cancellationToken);

        return result.Response;
    }

    public async Task<PayOsWebhookResponse> ConfirmConsultationPaymentByOrderCodeAsync(
        long orderCode,
        CancellationToken cancellationToken = default)
    {
        var transaction = await FindConsultationTransactionByOrderCodeAsync(orderCode, true, cancellationToken);
        if (transaction == null)
        {
            throw new InvalidOperationException($"Consultation transaction with orderCode {orderCode} was not found.");
        }

        var response = await ConfirmConsultationPaymentAsync(transaction.Id, cancellationToken);
        return BuildWebhookSuccessResponse(response);
    }

    public async Task<PayOsWebhookResponse> ProcessConsultationWebhookAsync(
        string rawPayload,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawPayload))
        {
            throw new ArgumentException("Webhook payload cannot be empty.", nameof(rawPayload));
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

        var result = await ProcessConfirmedPayOsPaymentAsync(webhook, cancellationToken);
        return BuildWebhookSuccessResponse(result.Response);
    }

    public async Task<bool> IsConsultationPayOsOrderCodeAsync(
        long orderCode,
        CancellationToken cancellationToken = default)
    {
        var transaction = await FindConsultationTransactionByOrderCodeAsync(orderCode, false, cancellationToken);
        return transaction != null;
    }

    public async Task<bool> RefundEmergencyEscrowAsync(
        Guid requestId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var ping = await _unitOfWork.GetRepository<ConsultationPingRequest>().FirstOrDefaultAsync(
                predicate: p => p.Id == requestId,
                asNoTracking: false,
                cancellationToken: cancellationToken);

            if (ping == null)
            {
                throw new NotFoundException("Emergency consultation request was not found.");
            }

            var existingRefund = await FindTransactionAsync(requestId, TransactionType.ConsultationRefund, cancellationToken);
            if (existingRefund != null)
            {
                return false;
            }

            var paymentTransaction = await RequireSuccessfulConsultationPaymentAsync(requestId, cancellationToken);
            await RefundFromEscrowAsync(ping.RescuerId, requestId, paymentTransaction.Amount, reason, cancellationToken);
            return true;
        });
    }

    public async Task<bool> RefundScheduledBookingAsync(
        Guid bookingId,
        Guid receiverId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var booking = await _unitOfWork.GetRepository<ConsultationBooking>().FirstOrDefaultAsync(
                predicate: b => b.Id == bookingId,
                asNoTracking: true,
                cancellationToken: cancellationToken);

            if (booking == null)
            {
                throw new NotFoundException("Consultation booking was not found.");
            }

            if (receiverId != booking.UserId)
            {
                throw new ValidationException("Scheduled booking refunds must be sent to the booking owner.");
            }

            var existingRefund = await FindTransactionAsync(bookingId, TransactionType.ConsultationRefund, cancellationToken);
            if (existingRefund != null)
            {
                return false;
            }

            var paymentTransaction = await RequireSuccessfulConsultationPaymentAsync(bookingId, cancellationToken);
            await RefundFromEscrowAsync(booking.UserId, bookingId, paymentTransaction.Amount, reason, cancellationToken);
            return true;
        });
    }

    public async Task<bool> CancelPendingScheduledBookingPaymentAsync(
        Guid bookingId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var pendingOrderCode = 0L;
        var shouldCancelGatewayLink = false;

        var cancelled = await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var paymentTransaction = await FindTransactionAsync(bookingId, TransactionType.ConsultationPayment, cancellationToken);
            if (paymentTransaction == null)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(paymentTransaction.ExternalTransactionId))
            {
                throw new ConflictException(
                    $"Scheduled booking payment has already been confirmed with external transaction '{paymentTransaction.ExternalTransactionId}'.");
            }

            if (!string.Equals(paymentTransaction.PaymentMethod, "PayOS", StringComparison.OrdinalIgnoreCase))
            {
                _unitOfWork.GetRepository<Transaction>().Delete(paymentTransaction);
                return true;
            }

            pendingOrderCode = ExtractOrderCodeFromDescription(paymentTransaction.Description);
            shouldCancelGatewayLink = pendingOrderCode > 0;
            _unitOfWork.GetRepository<Transaction>().Delete(paymentTransaction);
            return true;
        });

        if (cancelled && shouldCancelGatewayLink)
        {
            try
            {
                await _paymentGateway.CancelPaymentLinkAsync(pendingOrderCode, reason, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to cancel PayOS link {OrderCode} for scheduled booking {BookingId}",
                    pendingOrderCode,
                    bookingId);
            }
        }

        return cancelled;
    }

    public async Task<int> ExpireEmergencyRequestsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var pendingRequests = await _unitOfWork.GetRepository<ConsultationPingRequest>().GetListAsync(
            predicate: p =>
                (p.Status == ConsultationPingStatus.PendingExpertResponse
                 || p.Status == ConsultationPingStatus.PendingPayment)
                && (
                    (p.ExpiresAt.HasValue && p.ExpiresAt.Value <= now)
                    || (!p.ExpiresAt.HasValue && p.RequestedAt <= now.Add(-EmergencyRequestTtl))
                ),
            asNoTracking: false,
            cancellationToken: cancellationToken);

        var expiredCount = 0;
        foreach (var ping in pendingRequests)
        {
            var shouldRefund = ping.Status == ConsultationPingStatus.PendingExpertResponse;
            ping.Status = ConsultationPingStatus.Expired;
            ping.RespondedAt = now;
            _unitOfWork.GetRepository<ConsultationPingRequest>().Update(ping);
            await _unitOfWork.CommitAsync();

            if (shouldRefund)
            {
                await RefundEmergencyEscrowAsync(ping.Id, "Emergency consultation request expired.", cancellationToken);
            }
            await _notificationService.NotifyEmergencyRequestStatusChangedAsync(
                ping.Id,
                new
                {
                    requestId = ping.Id,
                    requesterId = ping.RescuerId,
                    expertId = ping.ExpertId,
                    status = ConsultationPingStatus.Expired,
                    requestedAt = ping.RequestedAt,
                    expiresAt = ping.ExpiresAt,
                    respondedAt = ping.RespondedAt,
                    consultationId = ping.ConsultationId,
                    roomId = (string?)null
                });

            if (_notificationQueueService != null)
            {
                await _notificationQueueService.PublishAsync(new NotificationMessage
                {
                    UserId = ping.RescuerId,
                    Title = "Yêu cầu tư vấn khẩn cấp đã hết hạn",
                    Body = "Yêu cầu tư vấn khẩn cấp của bạn đã hết hạn và được hoàn tiền.",
                    Type = "EMERGENCY_CONSULTATION_EXPIRED",
                    Data = new Dictionary<string, string>
                    {
                        ["requestId"] = ping.Id.ToString(),
                        ["consultationId"] = ping.ConsultationId.ToString()
                    }
                }, cancellationToken);
            }

            expiredCount++;
        }

        return expiredCount;
    }

    public async Task<bool> SettleConsultationEscrowAsync(Guid consultationId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var existingPayout = await FindTransactionAsync(consultationId, TransactionType.ExpertPayout, cancellationToken);
            if (existingPayout != null)
            {
                return false;
            }

            var consultation = await _unitOfWork.GetRepository<Consultation>().FirstOrDefaultAsync(
                predicate: c => c.Id == consultationId,
                asNoTracking: true,
                cancellationToken: cancellationToken);

            if (consultation == null)
            {
                throw new NotFoundException("Consultation not found for settlement.");
            }

            decimal amount;
            Guid paymentReferenceId;
            var scheduledBooking = await _unitOfWork.GetRepository<ConsultationBooking>().FirstOrDefaultAsync(
                predicate: b => b.ConsultationId == consultationId,
                asNoTracking: true,
                cancellationToken: cancellationToken);

            if (scheduledBooking != null)
            {
                paymentReferenceId = scheduledBooking.Id;
                amount = (await RequireSuccessfulConsultationPaymentAsync(paymentReferenceId, cancellationToken)).Amount;
            }
            else
            {
                var ping = await _unitOfWork.GetRepository<ConsultationPingRequest>().FirstOrDefaultAsync(
                    predicate: p => p.ConsultationId == consultationId,
                    asNoTracking: true,
                    cancellationToken: cancellationToken);

                if (ping == null)
                {
                    return false;
                }

                paymentReferenceId = ping.Id;
                amount = (await RequireSuccessfulConsultationPaymentAsync(paymentReferenceId, cancellationToken)).Amount;
            }

            await TransferEscrowToExpertAsync(consultation.CalleeId, consultationId, paymentReferenceId, amount, cancellationToken);
            return true;
        });
    }

    private async Task<ConsultationPaymentResponse> PayScheduledBookingWithWalletAsync(
        Guid userId,
        Guid bookingId,
        ProcessConsultationPaymentRequest request,
        CancellationToken cancellationToken)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var bookingRepo = _unitOfWork.GetRepository<ConsultationBooking>();
            var booking = await bookingRepo.FirstOrDefaultAsync(
                predicate: b => b.Id == bookingId,
                include: q => q.Include(b => b.Consultation),
                asNoTracking: false,
                cancellationToken: cancellationToken);

            if (booking == null)
            {
                throw new NotFoundException("Consultation booking was not found.");
            }

            if (booking.UserId != userId)
            {
                throw new ForbiddenException("You are not allowed to pay for this booking.");
            }

            if (booking.Status != BookingStatus.PendingPayment)
            {
                throw new ConflictException("Consultation booking is no longer waiting for payment.");
            }

            var paymentTransaction = await FindTransactionAsync(booking.Id, TransactionType.ConsultationPayment, cancellationToken);
            if (paymentTransaction != null)
            {
                throw new ConflictException("Consultation booking has already been paid.");
            }

            var transfer = await MoveMoneyToEscrowAsync(
                userId,
                booking.Id,
                booking.Price,
                TransactionType.ConsultationPayment,
                "Scheduled consultation payment",
                "Wallet",
                $"WALLET-{Guid.NewGuid():N}",
                cancellationToken);

            booking.Status = BookingStatus.Confirmed;
            bookingRepo.Update(booking);
            await _unitOfWork.CommitAsync();

            return new ConsultationPaymentResponse
            {
                ReferenceId = booking.Id,
                ReferenceType = ConsultationPaymentReferenceType.ScheduledBooking,
                TransactionId = transfer.TransactionId,
                Amount = booking.Price,
                Currency = "VND",
                PaymentMethod = request.PaymentMethod,
                Status = "Escrowed",
                UserWalletBalanceAfter = transfer.UserWalletBalanceAfter,
                PaidAtUtc = transfer.ProcessedAtUtc,
                Provider = "Wallet",
                ExternalTransactionId = transfer.ExternalTransactionId
            };
        });
    }

    private async Task<ConsultationPaymentResponse> PayEmergencyRequestWithWalletAsync(
        Guid userId,
        Guid requestId,
        ProcessConsultationPaymentRequest request,
        CancellationToken cancellationToken)
    {
        ConsultationPaymentResponse response;
        Guid expertId;
        DateTime requestedAt;
        DateTime expiresAt;

        (response, expertId, requestedAt, expiresAt) = await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var pingRepo = _unitOfWork.GetRepository<ConsultationPingRequest>();
            var ping = await pingRepo.FirstOrDefaultAsync(
                predicate: p => p.Id == requestId,
                asNoTracking: false,
                cancellationToken: cancellationToken);

            if (ping == null)
            {
                throw new NotFoundException("Emergency consultation request was not found.");
            }

            if (ping.RescuerId != userId)
            {
                throw new ForbiddenException("You are not allowed to pay for this emergency request.");
            }

            if (ping.Status != ConsultationPingStatus.PendingPayment)
            {
                throw new ConflictException("Emergency consultation request is no longer waiting for payment.");
            }

            if (!_notificationService.IsExpertConnected(ping.ExpertId.ToString()))
            {
                throw new ConflictException("Selected expert is currently offline for immediate consultation.");
            }

            var duplicatePayment = await FindTransactionAsync(ping.Id, TransactionType.ConsultationPayment, cancellationToken);
            if (duplicatePayment != null)
            {
                throw new ConflictException("Emergency consultation request has already been paid.");
            }

            var emergencyFee = await GetEmergencyFeeAsync(ping.ExpertId, cancellationToken);
            var transfer = await MoveMoneyToEscrowAsync(
                userId,
                ping.Id,
                emergencyFee,
                TransactionType.ConsultationPayment,
                "Emergency consultation payment",
                "Wallet",
                $"WALLET-{Guid.NewGuid():N}",
                cancellationToken);

            requestedAt = DateTime.UtcNow;
            expiresAt = requestedAt.Add(EmergencyRequestTtl);
            ping.RequestedAt = requestedAt;
            ping.ExpiresAt = expiresAt;
            ping.Status = ConsultationPingStatus.PendingExpertResponse;
            pingRepo.Update(ping);
            await _unitOfWork.CommitAsync();

            return (
                new ConsultationPaymentResponse
                {
                    ReferenceId = ping.Id,
                    ReferenceType = ConsultationPaymentReferenceType.EmergencyRequest,
                    TransactionId = transfer.TransactionId,
                    Amount = emergencyFee,
                    Currency = "VND",
                    PaymentMethod = request.PaymentMethod,
                    Status = "Escrowed",
                    UserWalletBalanceAfter = transfer.UserWalletBalanceAfter,
                    PaidAtUtc = transfer.ProcessedAtUtc,
                    Provider = "Wallet",
                    ExternalTransactionId = transfer.ExternalTransactionId
                },
                ping.ExpertId,
                requestedAt,
                expiresAt);
        });

        await _notificationService.SendEmergencyRequestAsync(
            expertId.ToString(),
            new
            {
                requestId,
                requesterId = userId,
                expertId,
                requestedAt,
                expiresAt
            });

        return response;
    }

    private async Task<ConsultationPaymentResponse> CreateScheduledBookingPayOsIntentAsync(
        Guid userId,
        Guid bookingId,
        ProcessConsultationPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var booking = await _unitOfWork.GetRepository<ConsultationBooking>().FirstOrDefaultAsync(
            predicate: b => b.Id == bookingId,
            include: q => q.Include(b => b.Consultation),
            asNoTracking: false,
            cancellationToken: cancellationToken);

        if (booking == null)
        {
            throw new NotFoundException("Consultation booking was not found.");
        }

        if (booking.UserId != userId)
        {
            throw new ForbiddenException("You are not allowed to pay for this booking.");
        }

        if (booking.Status != BookingStatus.PendingPayment)
        {
            throw new ConflictException("Consultation booking is no longer waiting for payment.");
        }

        var pendingTransaction = await PreparePendingPayOsTransactionAsync(
            userId,
            booking.Id,
            booking.Price,
            "Scheduled consultation payment",
            cancellationToken);

        var payOsResult = await CreatePayOsLinkAsync(
            pendingTransaction.OrderCode,
            booking.Price,
            "Scheduled consultation payment",
            cancellationToken);

        return BuildPendingResponse(
            booking.Id,
            ConsultationPaymentReferenceType.ScheduledBooking,
            pendingTransaction.TransactionId,
            booking.Price,
            request.PaymentMethod,
            payOsResult);
    }

    private async Task<ConsultationPaymentResponse> CreateEmergencyRequestPayOsIntentAsync(
        Guid userId,
        Guid requestId,
        ProcessConsultationPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var ping = await _unitOfWork.GetRepository<ConsultationPingRequest>().FirstOrDefaultAsync(
            predicate: p => p.Id == requestId,
            asNoTracking: false,
            cancellationToken: cancellationToken);

        if (ping == null)
        {
            throw new NotFoundException("Emergency consultation request was not found.");
        }

        if (ping.RescuerId != userId)
        {
            throw new ForbiddenException("You are not allowed to pay for this emergency request.");
        }

        if (ping.Status != ConsultationPingStatus.PendingPayment)
        {
            throw new ConflictException("Emergency consultation request is no longer waiting for payment.");
        }

        if (!_notificationService.IsExpertConnected(ping.ExpertId.ToString()))
        {
            throw new ConflictException("Selected expert is currently offline for immediate consultation.");
        }

        var emergencyFee = await GetEmergencyFeeAsync(ping.ExpertId, cancellationToken);
        var pendingTransaction = await PreparePendingPayOsTransactionAsync(
            userId,
            ping.Id,
            emergencyFee,
            "Emergency consultation payment",
            cancellationToken);

        var payOsResult = await CreatePayOsLinkAsync(
            pendingTransaction.OrderCode,
            emergencyFee,
            "Emergency consultation payment",
            cancellationToken);

        return BuildPendingResponse(
            ping.Id,
            ConsultationPaymentReferenceType.EmergencyRequest,
            pendingTransaction.TransactionId,
            emergencyFee,
            request.PaymentMethod,
            payOsResult);
    }

    private async Task<PendingPayOsTransactionContext> PreparePendingPayOsTransactionAsync(
        Guid userId,
        Guid referenceId,
        decimal amount,
        string description,
        CancellationToken cancellationToken)
    {
        var existingTransaction = await FindTransactionAsync(referenceId, TransactionType.ConsultationPayment, cancellationToken);
        if (existingTransaction != null)
        {
            if (!string.IsNullOrWhiteSpace(existingTransaction.ExternalTransactionId))
            {
                throw new ConflictException("Consultation has already been paid.");
            }

            if (!string.Equals(existingTransaction.PaymentMethod, "PayOS", StringComparison.OrdinalIgnoreCase))
            {
                throw new ConflictException("Consultation already has a pending payment transaction.");
            }

            var oldOrderCode = ExtractOrderCodeFromDescription(existingTransaction.Description);
            if (oldOrderCode > 0)
            {
                try
                {
                    await _paymentGateway.CancelPaymentLinkAsync(oldOrderCode, "Replacing old consultation payment link.", cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to cancel old PayOS payment link for consultation reference {ReferenceId}", referenceId);
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
            ReferenceId = referenceId,
            Amount = amount,
            Currency = "VND",
            TransactionType = TransactionType.ConsultationPayment,
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

    private async Task<PayOsPaymentLinkResult> CreatePayOsLinkAsync(
        long orderCode,
        decimal amount,
        string description,
        CancellationToken cancellationToken)
    {
        var payOsResult = await _paymentGateway.CreatePaymentLinkAsync(
            new PayOsCreatePaymentRequest
            {
                OrderCode = orderCode,
                Amount = amount,
                Description = BuildDescription(orderCode, description),
                ItemName = "Consultation Payment",
                Quantity = 1
            },
            cancellationToken);

        if (!payOsResult.Success)
        {
            var pendingTransaction = await FindConsultationTransactionByOrderCodeAsync(orderCode, false, cancellationToken);
            if (pendingTransaction != null && string.IsNullOrWhiteSpace(pendingTransaction.ExternalTransactionId))
            {
                _unitOfWork.GetRepository<Transaction>().Delete(pendingTransaction);
                await _unitOfWork.CommitAsync();
            }

            throw new ConflictException(payOsResult.ErrorMessage ?? "Failed to create PayOS payment link.");
        }

        return payOsResult;
    }

    private async Task<PayOsProcessResult> ProcessConfirmedPayOsPaymentAsync(
        PayOsWebhookData webhook,
        CancellationToken cancellationToken)
    {
        var context = await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var transaction = await FindConsultationTransactionByOrderCodeAsync(webhook.OrderCode, false, cancellationToken);
            if (transaction == null)
            {
                throw new InvalidOperationException($"Consultation payment transaction with orderCode {webhook.OrderCode} was not found.");
            }

            EnsureConsultationPayOsTransaction(transaction);

            if (!string.IsNullOrWhiteSpace(transaction.ExternalTransactionId))
            {
                var confirmedResponse = await BuildConfirmedResponseAsync(transaction, cancellationToken);
                return new ConfirmedPayOsContext
                {
                    Response = confirmedResponse
                };
            }

            transaction.ExternalTransactionId = string.IsNullOrWhiteSpace(webhook.TransactionReference)
                ? $"CONSULT-WEBHOOK-{transaction.Id:N}"
                : webhook.TransactionReference;
            transaction.CreatedAt = webhook.TransactionDateTime ?? DateTime.UtcNow;
            _unitOfWork.GetRepository<Transaction>().Update(transaction);

            var payerUserId = transaction.UserId
                ?? throw new ConflictException("Consultation payment transaction is missing payer user ownership.");

            var booking = await _unitOfWork.GetRepository<ConsultationBooking>().FirstOrDefaultAsync(
                predicate: b => b.Id == transaction.ReferenceId,
                include: q => q.Include(b => b.Consultation),
                asNoTracking: false,
                cancellationToken: cancellationToken);

            if (booking != null)
            {
                if (booking.Status != BookingStatus.PendingPayment)
                {
                    throw new ConflictException("Consultation booking is no longer waiting for payment.");
                }

                var escrowTransfer = await MoveMoneyToEscrowAsync(
                    payerUserId,
                    transaction.ReferenceId,
                    transaction.Amount,
                    TransactionType.ConsultationPayment,
                    "Consultation payment via PayOS",
                    "PayOS",
                    transaction.ExternalTransactionId,
                    cancellationToken,
                    skipExistingPaymentInsert: true);

                booking.Status = BookingStatus.Confirmed;
                _unitOfWork.GetRepository<ConsultationBooking>().Update(booking);
                await _unitOfWork.CommitAsync();

                return new ConfirmedPayOsContext
                {
                    Response = new ConsultationPaymentResponse
                    {
                        ReferenceId = booking.Id,
                        ReferenceType = ConsultationPaymentReferenceType.ScheduledBooking,
                        TransactionId = transaction.Id,
                        Amount = transaction.Amount,
                        Currency = transaction.Currency,
                        PaymentMethod = ConsultationPaymentMethod.PayOs,
                        Status = "Escrowed",
                        UserWalletBalanceAfter = escrowTransfer.UserWalletBalanceAfter,
                        PaidAtUtc = escrowTransfer.ProcessedAtUtc,
                        Provider = "PayOS",
                        OrderCode = webhook.OrderCode,
                        PaymentLinkId = webhook.PaymentLinkId,
                        ExternalTransactionId = transaction.ExternalTransactionId
                    },
                };
            }

            var ping = await _unitOfWork.GetRepository<ConsultationPingRequest>().FirstOrDefaultAsync(
                predicate: p => p.Id == transaction.ReferenceId,
                asNoTracking: false,
                cancellationToken: cancellationToken);

            if (ping == null)
            {
                throw new InvalidOperationException("Consultation payment reference could not be resolved.");
            }

            if (ping.Status != ConsultationPingStatus.PendingPayment)
            {
                throw new ConflictException("Emergency consultation request is no longer waiting for payment.");
            }

            var emergencyEscrowTransfer = await MoveMoneyToEscrowAsync(
                payerUserId,
                transaction.ReferenceId,
                transaction.Amount,
                TransactionType.ConsultationPayment,
                "Consultation payment via PayOS",
                "PayOS",
                transaction.ExternalTransactionId,
                cancellationToken,
                skipExistingPaymentInsert: true);

            var requestedAt = DateTime.UtcNow;
            var expiresAt = requestedAt.Add(EmergencyRequestTtl);
            ping.RequestedAt = requestedAt;
            ping.ExpiresAt = expiresAt;
            ping.Status = ConsultationPingStatus.PendingExpertResponse;
            _unitOfWork.GetRepository<ConsultationPingRequest>().Update(ping);
            await _unitOfWork.CommitAsync();

            return new ConfirmedPayOsContext
            {
                Response = new ConsultationPaymentResponse
                {
                    ReferenceId = ping.Id,
                    ReferenceType = ConsultationPaymentReferenceType.EmergencyRequest,
                    TransactionId = transaction.Id,
                    Amount = transaction.Amount,
                    Currency = transaction.Currency,
                    PaymentMethod = ConsultationPaymentMethod.PayOs,
                    Status = "Escrowed",
                    UserWalletBalanceAfter = emergencyEscrowTransfer.UserWalletBalanceAfter,
                    PaidAtUtc = emergencyEscrowTransfer.ProcessedAtUtc,
                    Provider = "PayOS",
                    OrderCode = webhook.OrderCode,
                    PaymentLinkId = webhook.PaymentLinkId,
                    ExternalTransactionId = transaction.ExternalTransactionId
                },
                ExpertId = ping.ExpertId,
                RequestedAt = requestedAt,
                ExpiresAt = expiresAt
            };
        });

        if (context.ExpertId.HasValue)
        {
            await _notificationService.SendEmergencyRequestAsync(
                context.ExpertId.Value.ToString(),
                new
                {
                    requestId = context.Response.ReferenceId,
                    requesterId = await GetRequesterIdAsync(context.Response.ReferenceId, cancellationToken),
                    expertId = context.ExpertId.Value,
                    requestedAt = context.RequestedAt,
                    expiresAt = context.ExpiresAt
                });
        }

        return new PayOsProcessResult { Response = context.Response };
    }

    private async Task<Guid> GetRequesterIdAsync(Guid requestId, CancellationToken cancellationToken)
    {
        var ping = await _unitOfWork.GetRepository<ConsultationPingRequest>().FirstOrDefaultAsync(
            predicate: p => p.Id == requestId,
            asNoTracking: true,
            cancellationToken: cancellationToken);

        if (ping == null)
        {
            throw new NotFoundException("Emergency consultation request was not found.");
        }

        return ping.RescuerId;
    }

    private static ConsultationPaymentResponse BuildPendingResponse(
        Guid referenceId,
        ConsultationPaymentReferenceType referenceType,
        Guid transactionId,
        decimal amount,
        ConsultationPaymentMethod paymentMethod,
        PayOsPaymentLinkResult payOsResult)
    {
        return new ConsultationPaymentResponse
        {
            ReferenceId = referenceId,
            ReferenceType = referenceType,
            TransactionId = transactionId,
            Amount = amount,
            Currency = "VND",
            PaymentMethod = paymentMethod,
            Status = "Pending",
            Provider = "PayOS",
            CheckoutUrl = payOsResult.CheckoutUrl,
            OrderCode = payOsResult.OrderCode,
            PaymentLinkId = payOsResult.PaymentLinkId
        };
    }

    private async Task<ConsultationPaymentResponse> BuildConfirmedResponseAsync(
        Transaction transaction,
        CancellationToken cancellationToken)
    {
        var userWallet = await _unitOfWork.GetRepository<Wallet>().FirstOrDefaultAsync(
            predicate: w => w.UserId == transaction.UserId,
            asNoTracking: true,
            cancellationToken: cancellationToken);

        var booking = await _unitOfWork.GetRepository<ConsultationBooking>().FirstOrDefaultAsync(
            predicate: b => b.Id == transaction.ReferenceId,
            asNoTracking: true,
            cancellationToken: cancellationToken);

        if (booking != null)
        {
            return new ConsultationPaymentResponse
            {
                ReferenceId = booking.Id,
                ReferenceType = ConsultationPaymentReferenceType.ScheduledBooking,
                TransactionId = transaction.Id,
                Amount = transaction.Amount,
                Currency = transaction.Currency,
                PaymentMethod = string.Equals(transaction.PaymentMethod, "PayOS", StringComparison.OrdinalIgnoreCase)
                    ? ConsultationPaymentMethod.PayOs
                    : ConsultationPaymentMethod.WalletBalance,
                Status = "Escrowed",
                UserWalletBalanceAfter = userWallet?.Balance,
                PaidAtUtc = transaction.CreatedAt,
                Provider = transaction.PaymentMethod,
                OrderCode = ExtractOrderCodeFromDescription(transaction.Description),
                ExternalTransactionId = transaction.ExternalTransactionId
            };
        }

        var ping = await _unitOfWork.GetRepository<ConsultationPingRequest>().FirstOrDefaultAsync(
            predicate: p => p.Id == transaction.ReferenceId,
            asNoTracking: true,
            cancellationToken: cancellationToken);

        if (ping == null)
        {
            throw new InvalidOperationException("Consultation payment reference could not be resolved.");
        }

        return new ConsultationPaymentResponse
        {
            ReferenceId = ping.Id,
            ReferenceType = ConsultationPaymentReferenceType.EmergencyRequest,
            TransactionId = transaction.Id,
            Amount = transaction.Amount,
            Currency = transaction.Currency,
            PaymentMethod = string.Equals(transaction.PaymentMethod, "PayOS", StringComparison.OrdinalIgnoreCase)
                ? ConsultationPaymentMethod.PayOs
                : ConsultationPaymentMethod.WalletBalance,
            Status = "Escrowed",
            UserWalletBalanceAfter = userWallet?.Balance,
            PaidAtUtc = transaction.CreatedAt,
            Provider = transaction.PaymentMethod,
            OrderCode = ExtractOrderCodeFromDescription(transaction.Description),
            ExternalTransactionId = transaction.ExternalTransactionId
        };
    }

    private static PayOsWebhookResponse BuildWebhookSuccessResponse(ConsultationPaymentResponse response)
    {
        return new PayOsWebhookResponse
        {
            Success = true,
            Message = "Consultation payment confirmed successfully",
            TransactionId = response.TransactionId,
            OrderCode = response.OrderCode ?? 0,
            Amount = Convert.ToInt32(Math.Round(response.Amount, MidpointRounding.AwayFromZero)),
            TransactionReference = response.ExternalTransactionId ?? string.Empty,
            TransactionDateTime = response.PaidAtUtc
        };
    }

    private async Task<decimal> GetEmergencyFeeAsync(Guid expertId, CancellationToken cancellationToken)
    {
        var expertProfile = await _unitOfWork.GetRepository<ExpertProfile>().FirstOrDefaultAsync(
            predicate: p => p.AccountId == expertId,
            asNoTracking: true,
            cancellationToken: cancellationToken);

        if (expertProfile == null)
        {
            throw new NotFoundException("Expert profile was not found.");
        }

        return expertProfile.EmergencyConsultationFee ?? expertProfile.ConsultationFee;
    }

    private static bool IsPaymentLinkPaid(PayOsLinkInformation linkInfo)
    {
        return linkInfo.Status.Equals("PAID", StringComparison.OrdinalIgnoreCase)
               || (linkInfo.AmountPaid > 0 && linkInfo.AmountPaid >= linkInfo.Amount);
    }

    private static void EnsureConsultationPayOsTransaction(Transaction transaction)
    {
        if (transaction.TransactionType != TransactionType.ConsultationPayment)
        {
            throw new ValidationException("Transaction is not a consultation payment.");
        }

        if (!string.Equals(transaction.PaymentMethod, "PayOS", StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException("Transaction is not a PayOS consultation payment.");
        }
    }

    private async Task<Transaction?> FindConsultationTransactionByOrderCodeAsync(
        long orderCode,
        bool asNoTracking,
        CancellationToken cancellationToken)
    {
        var descriptionPrefix = PayOsPaymentFlowPrefixes.BuildOrderCodePrefix(PayOsPaymentFlow.Consultation, orderCode);
        return await _unitOfWork.GetRepository<Transaction>().FirstOrDefaultAsync(
            predicate: t => t.TransactionType == TransactionType.ConsultationPayment
                         && t.Description != null
                         && t.Description.StartsWith(descriptionPrefix)
                         && t.PaymentMethod == "PayOS",
            asNoTracking: asNoTracking,
            cancellationToken: cancellationToken);
    }

    private static long GenerateOrderCode()
    {
        // PayOS descriptions are capped at 25 chars, so keep generated order codes short enough for the prefix.
        // timestamp (10 digits) + random (4 digits) = 14 digits
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var randomPart = System.Security.Cryptography.RandomNumberGenerator.GetInt32(1000, 9999);
        return long.Parse($"{timestamp}{randomPart}");
    }

    private static string BuildDescription(long orderCode, string description)
    {
        const int maxLength = 25;
        var baseDescription = PayOsPaymentFlowPrefixes.BuildOrderCodePrefix(PayOsPaymentFlow.Consultation, orderCode);

        if (baseDescription.Length >= maxLength || string.IsNullOrWhiteSpace(description))
        {
            return baseDescription.Length <= maxLength
                ? baseDescription
                : baseDescription[..maxLength];
        }

        var combined = $"{baseDescription} {description}";
        return combined.Length <= maxLength
            ? combined
            : baseDescription;
    }

    private static long ExtractOrderCodeFromDescription(string? description)
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

    private async Task<Transaction?> FindTransactionAsync(Guid referenceId, TransactionType transactionType, CancellationToken cancellationToken)
    {
        return await _unitOfWork.GetRepository<Transaction>().FirstOrDefaultAsync(
            predicate: t => t.ReferenceId == referenceId && t.TransactionType == transactionType,
            asNoTracking: false,
            cancellationToken: cancellationToken);
    }

    private async Task<Transaction> RequireSuccessfulConsultationPaymentAsync(Guid referenceId, CancellationToken cancellationToken)
    {
        var tx = await _unitOfWork.GetRepository<Transaction>().FirstOrDefaultAsync(
            predicate: t => t.ReferenceId == referenceId
                         && t.TransactionType == TransactionType.ConsultationPayment
                         && !string.IsNullOrEmpty(t.ExternalTransactionId),
            asNoTracking: true,
            cancellationToken: cancellationToken);

        if (tx == null)
        {
            throw new ConflictException("Consultation payment was not found for escrow handling.");
        }

        return tx;
    }

    private async Task<(Guid TransactionId, decimal UserWalletBalanceAfter, DateTime ProcessedAtUtc, string ExternalTransactionId)> MoveMoneyToEscrowAsync(
        Guid userId,
        Guid referenceId,
        decimal amount,
        TransactionType transactionType,
        string description,
        string paymentMethod,
        string externalTransactionId,
        CancellationToken cancellationToken,
        bool skipExistingPaymentInsert = false)
    {
        decimal userWalletBalanceAfter;
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

        Transaction paymentTx;
        if (skipExistingPaymentInsert)
        {
            paymentTx = await _unitOfWork.GetRepository<Transaction>().FirstOrDefaultAsync(
                predicate: t => t.ReferenceId == referenceId && t.TransactionType == transactionType,
                asNoTracking: false,
                cancellationToken: cancellationToken)
                ?? throw new ConflictException("Consultation payment transaction was not found.");
        }
        else
        {
            paymentTx = new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ReferenceId = referenceId,
                Amount = amount,
                Currency = "VND",
                TransactionType = transactionType,
                Description = description,
                PaymentMethod = paymentMethod,
                ExternalTransactionId = externalTransactionId,
                CreatedAt = now
            };

            await _unitOfWork.GetRepository<Transaction>().InsertAsync(paymentTx);
        }

        return (paymentTx.Id, userWalletBalanceAfter, now, externalTransactionId);
    }

    private async Task RefundFromEscrowAsync(
        Guid receiverId,
        Guid referenceId,
        decimal amount,
        string description,
        CancellationToken cancellationToken)
    {
        var available = await GetAvailableConsultationEscrowByPaymentReferenceAsync(referenceId, cancellationToken);
        if (available < amount)
        {
            throw new ConflictException("Consultation escrow balance is insufficient for refund.");
        }

        var receiverWallet = await GetOrCreateWalletAsync(receiverId, cancellationToken);
        receiverWallet.Balance += amount;
        _unitOfWork.GetRepository<Wallet>().Update(receiverWallet);

        await _unitOfWork.GetRepository<Transaction>().InsertAsync(new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = receiverId,
            ReferenceId = referenceId,
            Amount = amount,
            Currency = "VND",
            TransactionType = TransactionType.ConsultationRefund,
            Description = description,
            PaymentMethod = "Internal",
            ExternalTransactionId = $"REFUND-{Guid.NewGuid():N}",
            CreatedAt = DateTime.UtcNow
        });

        await _unitOfWork.CommitAsync();
    }

    private async Task TransferEscrowToExpertAsync(
        Guid expertId,
        Guid consultationId,
        Guid paymentReferenceId,
        decimal amount,
        CancellationToken cancellationToken)
    {
        var settlement = CalculateConsultationSettlementAmounts(amount);
        var available = await GetAvailableConsultationEscrowForSettlementAsync(consultationId, paymentReferenceId, cancellationToken);
        if (available < settlement.GrossAmount)
        {
            throw new ConflictException("Consultation escrow balance is insufficient for expert settlement.");
        }

        var expertWallet = await GetOrCreateWalletAsync(expertId, cancellationToken);
        expertWallet.Balance += settlement.ExpertNetAmount;
        _unitOfWork.GetRepository<Wallet>().Update(expertWallet);

        if (settlement.PlatformFeeAmount > 0m)
        {
            await _unitOfWork.GetRepository<Transaction>().InsertAsync(new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = null,
                ReferenceId = consultationId,
                Amount = settlement.PlatformFeeAmount,
                Currency = "VND",
                TransactionType = TransactionType.PlatformFee,
                Description = $"Consultation platform fee for consultation {consultationId}",
                PaymentMethod = "Internal",
                ExternalTransactionId = $"SETTLE-FEE-{Guid.NewGuid():N}",
                CreatedAt = DateTime.UtcNow
            });
        }

        await _unitOfWork.GetRepository<Transaction>().InsertAsync(new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = expertId,
            ReferenceId = consultationId,
            Amount = settlement.ExpertNetAmount,
            Currency = "VND",
            TransactionType = TransactionType.ExpertPayout,
            Description = $"Consultation settlement for consultation {consultationId}",
            PaymentMethod = "Internal",
            ExternalTransactionId = $"SETTLE-{Guid.NewGuid():N}",
            CreatedAt = DateTime.UtcNow
        });

        await _unitOfWork.CommitAsync();
    }

    private decimal ResolveConsultationPlatformFeePercent()
    {
        var configuredPercent = _systemSettingService?.GetSetting<decimal?>(
            SystemSettingKeys.ConsultationPlatformFeePercent,
            DefaultConsultationPlatformFeePercent);

        var feePercent = configuredPercent ?? DefaultConsultationPlatformFeePercent;
        if (feePercent < 0m || feePercent >= 1m)
        {
            _logger.LogWarning(
                "Consultation platform fee percent {FeePercent} is outside the safe range [0, 1). Using default {DefaultPercent}.",
                feePercent,
                DefaultConsultationPlatformFeePercent);
            return DefaultConsultationPlatformFeePercent;
        }

        return feePercent;
    }

    private ConsultationSettlementAmounts CalculateConsultationSettlementAmounts(decimal grossAmount)
    {
        if (grossAmount < 0m)
        {
            throw new ValidationException("Consultation settlement gross amount cannot be negative.");
        }

        var feePercent = ResolveConsultationPlatformFeePercent();
        var expertNetAmount = decimal.Ceiling(grossAmount * (1m - feePercent));
        var feeAmount = grossAmount - expertNetAmount;

        return new ConsultationSettlementAmounts(grossAmount, feePercent, feeAmount, expertNetAmount);
    }

    private readonly record struct ConsultationSettlementAmounts(
        decimal GrossAmount,
        decimal FeePercent,
        decimal PlatformFeeAmount,
        decimal ExpertNetAmount);

    private async Task<decimal> GetAvailableConsultationEscrowByPaymentReferenceAsync(
        Guid referenceId,
        CancellationToken cancellationToken)
    {
        var heldTransactions = await _unitOfWork.GetRepository<Transaction>().GetListAsync(
            predicate: t => t.ReferenceId == referenceId
                         && t.TransactionType == TransactionType.ConsultationPayment
                         && !string.IsNullOrEmpty(t.ExternalTransactionId),
            asNoTracking: true,
            cancellationToken: cancellationToken);

        var releasedTransactions = await _unitOfWork.GetRepository<Transaction>().GetListAsync(
            predicate: t => t.ReferenceId == referenceId
                         && (t.TransactionType == TransactionType.ConsultationRefund
                             || t.TransactionType == TransactionType.PlatformFee),
            asNoTracking: true,
            cancellationToken: cancellationToken);

        return heldTransactions.Sum(t => t.Amount) - releasedTransactions.Sum(t => t.Amount);
    }

    private async Task<decimal> GetAvailableConsultationEscrowForSettlementAsync(
        Guid consultationId,
        Guid paymentReferenceId,
        CancellationToken cancellationToken)
    {
        var heldTransactions = await _unitOfWork.GetRepository<Transaction>().GetListAsync(
            predicate: t => t.ReferenceId == paymentReferenceId
                         && t.TransactionType == TransactionType.ConsultationPayment
                         && !string.IsNullOrEmpty(t.ExternalTransactionId),
            asNoTracking: true,
            cancellationToken: cancellationToken);

        var releasedByPaymentReference = await _unitOfWork.GetRepository<Transaction>().GetListAsync(
            predicate: t => t.ReferenceId == paymentReferenceId
                         && t.TransactionType == TransactionType.ConsultationRefund,
            asNoTracking: true,
            cancellationToken: cancellationToken);

        var releasedByConsultation = await _unitOfWork.GetRepository<Transaction>().GetListAsync(
            predicate: t => t.ReferenceId == consultationId
                         && (t.TransactionType == TransactionType.ExpertPayout
                             || t.TransactionType == TransactionType.PlatformFee),
            asNoTracking: true,
            cancellationToken: cancellationToken);

        return heldTransactions.Sum(t => t.Amount)
               - releasedByPaymentReference.Sum(t => t.Amount)
               - releasedByConsultation.Sum(t => t.Amount);
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

    private sealed class PendingPayOsTransactionContext
    {
        public Guid TransactionId { get; init; }
        public long OrderCode { get; init; }
    }

    private sealed class PayOsProcessResult
    {
        public ConsultationPaymentResponse Response { get; init; } = null!;
    }

    private sealed class ConfirmedPayOsContext
    {
        public ConsultationPaymentResponse Response { get; init; } = null!;
        public Guid? ExpertId { get; init; }
        public DateTime RequestedAt { get; init; }
        public DateTime ExpiresAt { get; init; }
    }
}
