using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Requests.Consultation;
using SnakeAid.Core.Responses.Consultation;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Helpers;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Service.Implements;

public class ConsultationPaymentService : IConsultationPaymentService
{
    private const string SystemWalletUserId = "57288b98-5f91-4de8-b827-866e3df69587";
    private static readonly TimeSpan EmergencyRequestTtl = TimeSpan.FromMinutes(2);

    private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
    private readonly IExpertEmergencyNotificationService _notificationService;
    private readonly ILogger<ConsultationPaymentService> _logger;

    public ConsultationPaymentService(
        IUnitOfWork<SnakeAidDbContext> unitOfWork,
        IExpertEmergencyNotificationService notificationService,
        ILogger<ConsultationPaymentService> logger)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<ConsultationPaymentResponse> PayScheduledBookingAsync(
        Guid userId,
        Guid bookingId,
        ProcessConsultationPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var paymentMethod = EnsureWalletPaymentMethod(request.PaymentMethod);

        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await PostgresAdvisoryLockHelper.AcquireTransactionLockAsync(
                _unitOfWork.Context,
                $"consultation:booking-payment:{bookingId}",
                cancellationToken);

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
                PaymentMethod = paymentMethod,
                Status = "Escrowed",
                UserWalletBalanceAfter = transfer.UserWalletBalanceAfter,
                SystemWalletBalanceAfter = transfer.SystemWalletBalanceAfter,
                PaidAtUtc = transfer.ProcessedAtUtc
            };
        });
    }

    public async Task<ConsultationPaymentResponse> PayEmergencyRequestAsync(
        Guid userId,
        Guid requestId,
        ProcessConsultationPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var paymentMethod = EnsureWalletPaymentMethod(request.PaymentMethod);

        ConsultationPaymentResponse response;
        Guid expertId;
        DateTime requestedAt;
        DateTime expiresAt;

        (response, expertId, requestedAt, expiresAt) = await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await PostgresAdvisoryLockHelper.AcquireTransactionLockAsync(
                _unitOfWork.Context,
                $"consultation:emergency-payment:{requestId}",
                cancellationToken);

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

            var expertProfile = await _unitOfWork.GetRepository<ExpertProfile>().FirstOrDefaultAsync(
                predicate: p => p.AccountId == ping.ExpertId,
                asNoTracking: true,
                cancellationToken: cancellationToken);

            if (expertProfile == null)
            {
                throw new NotFoundException("Expert profile was not found.");
            }

            var emergencyFee = expertProfile.EmergencyConsultationFee ?? expertProfile.ConsultationFee;
            var transfer = await MoveMoneyToEscrowAsync(
                userId,
                ping.Id,
                emergencyFee,
                TransactionType.ConsultationPayment,
                "Emergency consultation payment",
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
                    PaymentMethod = paymentMethod,
                    Status = "Escrowed",
                    UserWalletBalanceAfter = transfer.UserWalletBalanceAfter,
                    SystemWalletBalanceAfter = transfer.SystemWalletBalanceAfter,
                    PaidAtUtc = transfer.ProcessedAtUtc
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

    public async Task<bool> RefundEmergencyEscrowAsync(
        Guid requestId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await PostgresAdvisoryLockHelper.AcquireTransactionLockAsync(
                _unitOfWork.Context,
                $"consultation:refund:{requestId}",
                cancellationToken);

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

            var paymentTransaction = await RequireConsultationPaymentAsync(requestId, cancellationToken);
            await RefundFromEscrowAsync(ping.RescuerId, requestId, paymentTransaction.Amount, reason, cancellationToken);
            return true;
        });
    }

    public async Task<int> ExpireEmergencyRequestsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var pendingRequests = await _unitOfWork.GetRepository<ConsultationPingRequest>().GetListAsync(
            predicate: p => p.Status == ConsultationPingStatus.PendingExpertResponse
                && p.ExpiresAt.HasValue
                && p.ExpiresAt.Value <= now,
            asNoTracking: false,
            cancellationToken: cancellationToken);

        var expiredCount = 0;
        foreach (var ping in pendingRequests)
        {
            ping.Status = ConsultationPingStatus.Expired;
            ping.RespondedAt = now;
            _unitOfWork.GetRepository<ConsultationPingRequest>().Update(ping);
            await _unitOfWork.CommitAsync();

            await RefundEmergencyEscrowAsync(ping.Id, "Emergency consultation request expired.", cancellationToken);
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

            expiredCount++;
        }

        return expiredCount;
    }

    public async Task<bool> SettleConsultationEscrowAsync(Guid consultationId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await PostgresAdvisoryLockHelper.AcquireTransactionLockAsync(
                _unitOfWork.Context,
                $"consultation:settlement:{consultationId}",
                cancellationToken);

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
            var scheduledBooking = await _unitOfWork.GetRepository<ConsultationBooking>().FirstOrDefaultAsync(
                predicate: b => b.ConsultationId == consultationId,
                asNoTracking: true,
                cancellationToken: cancellationToken);

            if (scheduledBooking != null)
            {
                amount = (await RequireConsultationPaymentAsync(scheduledBooking.Id, cancellationToken)).Amount;
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

                amount = (await RequireConsultationPaymentAsync(ping.Id, cancellationToken)).Amount;
            }

            await TransferEscrowToExpertAsync(consultation.CalleeId, consultationId, amount, cancellationToken);
            return true;
        });
    }

    private static ConsultationPaymentMethod EnsureWalletPaymentMethod(ConsultationPaymentMethod? paymentMethod)
    {
        if (paymentMethod == null)
        {
            throw new ValidationException("PaymentMethod is required.");
        }

        if (paymentMethod != ConsultationPaymentMethod.WalletBalance)
        {
            throw new ValidationException("Only WalletBalance payment is supported in this MVP build.");
        }

        return paymentMethod.Value;
    }

    private async Task<Transaction?> FindTransactionAsync(Guid referenceId, TransactionType transactionType, CancellationToken cancellationToken)
    {
        return await _unitOfWork.GetRepository<Transaction>().FirstOrDefaultAsync(
            predicate: t => t.ReferenceId == referenceId && t.TransactionType == transactionType,
            asNoTracking: false,
            cancellationToken: cancellationToken);
    }

    private async Task<Transaction> RequireConsultationPaymentAsync(Guid referenceId, CancellationToken cancellationToken)
    {
        var tx = await _unitOfWork.GetRepository<Transaction>().FirstOrDefaultAsync(
            predicate: t => t.ReferenceId == referenceId && t.TransactionType == TransactionType.ConsultationPayment,
            asNoTracking: true,
            cancellationToken: cancellationToken);

        if (tx == null)
        {
            throw new ConflictException("Consultation payment was not found for escrow handling.");
        }

        return tx;
    }

    private async Task<(Guid TransactionId, decimal UserWalletBalanceAfter, decimal SystemWalletBalanceAfter, DateTime ProcessedAtUtc)> MoveMoneyToEscrowAsync(
        Guid userId,
        Guid referenceId,
        decimal amount,
        TransactionType transactionType,
        string description,
        CancellationToken cancellationToken)
    {
        var userWallet = await GetRequiredWalletAsync(userId, cancellationToken);
        if (userWallet.Balance < amount)
        {
            throw new ConflictException($"Insufficient wallet balance. Available: {userWallet.Balance}, required: {amount}.");
        }

        var systemWallet = await GetOrCreateWalletAsync(Guid.Parse(SystemWalletUserId), cancellationToken);
        var now = DateTime.UtcNow;

        userWallet.Balance -= amount;
        systemWallet.Balance += amount;
        _unitOfWork.GetRepository<Wallet>().Update(userWallet);
        _unitOfWork.GetRepository<Wallet>().Update(systemWallet);

        var paymentTx = new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ReferenceId = referenceId,
            Amount = amount,
            Currency = "VND",
            TransactionType = transactionType,
            Description = description,
            PaymentMethod = "Wallet",
            ExternalTransactionId = $"WALLET-{Guid.NewGuid():N}",
            CreatedAt = now
        };

        var systemCreditTx = new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = Guid.Parse(SystemWalletUserId),
            ReferenceId = referenceId,
            Amount = amount,
            Currency = "VND",
            TransactionType = TransactionType.WalletTopup,
            Description = $"Escrow received for consultation reference {referenceId}",
            PaymentMethod = "Internal",
            ExternalTransactionId = $"ESCROW-{Guid.NewGuid():N}",
            CreatedAt = now
        };

        await _unitOfWork.GetRepository<Transaction>().InsertAsync(paymentTx);
        await _unitOfWork.GetRepository<Transaction>().InsertAsync(systemCreditTx);

        return (paymentTx.Id, userWallet.Balance, systemWallet.Balance, now);
    }

    private async Task RefundFromEscrowAsync(
        Guid receiverId,
        Guid referenceId,
        decimal amount,
        string description,
        CancellationToken cancellationToken)
    {
        var systemWallet = await GetRequiredWalletAsync(Guid.Parse(SystemWalletUserId), cancellationToken);
        if (systemWallet.Balance < amount)
        {
            throw new ConflictException("System escrow balance is insufficient for refund.");
        }

        var receiverWallet = await GetOrCreateWalletAsync(receiverId, cancellationToken);
        systemWallet.Balance -= amount;
        receiverWallet.Balance += amount;
        _unitOfWork.GetRepository<Wallet>().Update(systemWallet);
        _unitOfWork.GetRepository<Wallet>().Update(receiverWallet);

        await _unitOfWork.GetRepository<Transaction>().InsertAsync(new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = Guid.Parse(SystemWalletUserId),
            ReferenceId = referenceId,
            Amount = amount,
            Currency = "VND",
            TransactionType = TransactionType.WalletWithdraw,
            Description = $"Escrow refund source for consultation reference {referenceId}",
            PaymentMethod = "Internal",
            ExternalTransactionId = $"REFUND-SOURCE-{Guid.NewGuid():N}",
            CreatedAt = DateTime.UtcNow
        });

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
        decimal amount,
        CancellationToken cancellationToken)
    {
        var systemWallet = await GetRequiredWalletAsync(Guid.Parse(SystemWalletUserId), cancellationToken);
        if (systemWallet.Balance < amount)
        {
            throw new ConflictException("System escrow balance is insufficient for expert settlement.");
        }

        var expertWallet = await GetOrCreateWalletAsync(expertId, cancellationToken);
        systemWallet.Balance -= amount;
        expertWallet.Balance += amount;
        _unitOfWork.GetRepository<Wallet>().Update(systemWallet);
        _unitOfWork.GetRepository<Wallet>().Update(expertWallet);

        await _unitOfWork.GetRepository<Transaction>().InsertAsync(new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = Guid.Parse(SystemWalletUserId),
            ReferenceId = consultationId,
            Amount = amount,
            Currency = "VND",
            TransactionType = TransactionType.WalletWithdraw,
            Description = $"Escrow settlement source for consultation {consultationId}",
            PaymentMethod = "Internal",
            ExternalTransactionId = $"SETTLE-SOURCE-{Guid.NewGuid():N}",
            CreatedAt = DateTime.UtcNow
        });

        await _unitOfWork.GetRepository<Transaction>().InsertAsync(new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = expertId,
            ReferenceId = consultationId,
            Amount = amount,
            Currency = "VND",
            TransactionType = TransactionType.ExpertPayout,
            Description = $"Consultation settlement for consultation {consultationId}",
            PaymentMethod = "Internal",
            ExternalTransactionId = $"SETTLE-{Guid.NewGuid():N}",
            CreatedAt = DateTime.UtcNow
        });

        await _unitOfWork.CommitAsync();
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
}
