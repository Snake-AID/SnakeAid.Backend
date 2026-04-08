using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using Moq;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Enums;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Requests.PayOs;
using SnakeAid.Core.Requests.PayOS;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Implements;
using SnakeAid.Service.Interfaces;
using SnakeAid.Service.Services.PayOs.Models;

namespace SnakeAid.Tests.Unit;

/// <summary>
/// Unit tests for SnakebiteIncidentPaymentService using Moq.
/// Tests validate exception paths and key behaviors via mocked dependencies.
/// </summary>
public class SnakebiteIncidentPaymentServiceTests
{
    private readonly Mock<IUnitOfWork<SnakeAidDbContext>> _unitOfWorkMock;
    private readonly Mock<IPaymentGateway> _paymentGatewayMock;
    private readonly Mock<ILogger<SnakebiteIncidentPaymentService>> _loggerMock;
    private readonly SnakebiteIncidentPaymentService _service;

    // Typed repository mocks
    private readonly Mock<IGenericRepository<SnakebiteIncident>> _incidentRepoMock;
    private readonly Mock<IGenericRepository<Transaction>> _transactionRepoMock;
    private readonly Mock<IGenericRepository<Wallet>> _walletRepoMock;

    private static readonly Guid TestUserId = Guid.NewGuid();
    private static readonly Guid TestIncidentId = Guid.NewGuid();
    private static readonly Guid SystemWalletUserId = Guid.Parse("57288b98-5f91-4de8-b827-866e3df69587");

    public SnakebiteIncidentPaymentServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork<SnakeAidDbContext>>();
        _paymentGatewayMock = new Mock<IPaymentGateway>();
        _loggerMock = new Mock<ILogger<SnakebiteIncidentPaymentService>>();

        _incidentRepoMock = new Mock<IGenericRepository<SnakebiteIncident>>();
        _transactionRepoMock = new Mock<IGenericRepository<Transaction>>();
        _walletRepoMock = new Mock<IGenericRepository<Wallet>>();

        _unitOfWorkMock
            .Setup(u => u.GetRepository<SnakebiteIncident>())
            .Returns(_incidentRepoMock.Object);
        _unitOfWorkMock
            .Setup(u => u.GetRepository<Transaction>())
            .Returns(_transactionRepoMock.Object);
        _unitOfWorkMock
            .Setup(u => u.GetRepository<Wallet>())
            .Returns(_walletRepoMock.Object);
        _unitOfWorkMock
            .Setup(u => u.CommitAsync())
            .ReturnsAsync(1);

        _service = new SnakebiteIncidentPaymentService(
            _unitOfWorkMock.Object,
            _paymentGatewayMock.Object,
            _loggerMock.Object);
    }

    #region Helpers

    private SnakebiteIncident CreateIncident(
        SnakebiteIncidentStatus status,
        Guid? userId = null,
        decimal missionCost = 100_000m)
    {
        return new SnakebiteIncident
        {
            Id = TestIncidentId,
            UserId = userId ?? TestUserId,
            Status = status,
            Missions = new List<RescueMission>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    IncidentId = TestIncidentId,
                    RescuerId = Guid.NewGuid(),
                    Status = RescueMissionStatus.MissionCompleted,
                    Price = missionCost,
                    ActualCost = null
                }
            }
        };
    }

    private CreateSnakebiteIncidentPaymentRequest CreatePaymentRequest(decimal amount = 100_000m)
    {
        return new CreateSnakebiteIncidentPaymentRequest
        {
            SnakebiteIncidentId = TestIncidentId,
            Amount = amount
        };
    }

    private void SetupIncidentRepo(SnakebiteIncident? incident)
    {
        _incidentRepoMock
            .Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<SnakebiteIncident, bool>>>(),
                It.IsAny<Func<IQueryable<SnakebiteIncident>, IOrderedQueryable<SnakebiteIncident>>>(),
                It.IsAny<Func<IQueryable<SnakebiteIncident>, IQueryable<SnakebiteIncident>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);
    }

    private void SetupTransactionRepo(Transaction? transaction)
    {
        _transactionRepoMock
            .Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<Transaction, bool>>>(),
                It.IsAny<Func<IQueryable<Transaction>, IOrderedQueryable<Transaction>>>(),
                It.IsAny<Func<IQueryable<Transaction>, IQueryable<Transaction>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);
    }

    private void SetupWalletRepo(Wallet? wallet)
    {
        _walletRepoMock
            .Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<Wallet, bool>>>(),
                It.IsAny<Func<IQueryable<Wallet>, IOrderedQueryable<Wallet>>>(),
                It.IsAny<Func<IQueryable<Wallet>, IQueryable<Wallet>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);
    }

    private void SetupWalletRepoFromList(ICollection<Wallet> wallets)
    {
        _walletRepoMock
            .Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<Wallet, bool>>>(),
                It.IsAny<Func<IQueryable<Wallet>, IOrderedQueryable<Wallet>>>(),
                It.IsAny<Func<IQueryable<Wallet>, IQueryable<Wallet>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((
                Expression<Func<Wallet, bool>> predicate,
                Func<IQueryable<Wallet>, IOrderedQueryable<Wallet>>? _1,
                Func<IQueryable<Wallet>, IQueryable<Wallet>>? _2,
                bool _3,
                CancellationToken _4) =>
            {
                var compiled = predicate.Compile();
                return wallets.FirstOrDefault(compiled);
            });
    }

    private void SetupTransactionRepoFromList(ICollection<Transaction> transactions)
    {
        _transactionRepoMock
            .Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<Transaction, bool>>>(),
                It.IsAny<Func<IQueryable<Transaction>, IOrderedQueryable<Transaction>>>(),
                It.IsAny<Func<IQueryable<Transaction>, IQueryable<Transaction>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((
                Expression<Func<Transaction, bool>> predicate,
                Func<IQueryable<Transaction>, IOrderedQueryable<Transaction>>? _1,
                Func<IQueryable<Transaction>, IQueryable<Transaction>>? _2,
                bool _3,
                CancellationToken _4) =>
            {
                var compiled = predicate.Compile();
                return transactions.FirstOrDefault(compiled);
            });

        _transactionRepoMock
            .Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<Transaction, bool>>>(),
                It.IsAny<Func<IQueryable<Transaction>, IOrderedQueryable<Transaction>>>(),
                It.IsAny<Func<IQueryable<Transaction>, IQueryable<Transaction>>>(),
                It.IsAny<int?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((
                Expression<Func<Transaction, bool>>? predicate,
                Func<IQueryable<Transaction>, IOrderedQueryable<Transaction>>? _1,
                Func<IQueryable<Transaction>, IQueryable<Transaction>>? _2,
                int? _3,
                bool _4,
                CancellationToken _5) =>
            {
                if (predicate == null)
                {
                    return transactions.ToList();
                }

                var compiled = predicate.Compile();
                return transactions.Where(compiled).ToList();
            });
    }

    #endregion

    #region Test 1: CreatePaymentLink_IncidentNotFinished_ThrowsConflict — Req 1.3

    /// <summary>
    /// Validates: Requirement 1.3
    /// WHEN incident is not in Finished status, service SHALL throw ConflictException.
    /// </summary>
    [Theory]
    [InlineData(SnakebiteIncidentStatus.Pending)]
    [InlineData(SnakebiteIncidentStatus.Verified)]
    [InlineData(SnakebiteIncidentStatus.Assigned)]
    public async Task CreatePaymentLink_IncidentNotFinished_ThrowsConflict(SnakebiteIncidentStatus status)
    {
        // Arrange
        var incident = CreateIncident(status);
        SetupIncidentRepo(incident);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(
            () => _service.CreateSnakebiteIncidentPaymentLinkAsync(
                CreatePaymentRequest(), TestUserId, CancellationToken.None));

        Assert.Contains("rescue mission is completed", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Test 2: CreatePaymentLink_AlreadyCompleted_ThrowsConflict — Req 1.8

    /// <summary>
    /// Validates: Requirement 1.8
    /// WHEN incident status is Completed, service SHALL throw ConflictException.
    /// </summary>
    [Fact]
    public async Task CreatePaymentLink_AlreadyCompleted_ThrowsConflict()
    {
        // Arrange
        var incident = CreateIncident(SnakebiteIncidentStatus.Completed);
        SetupIncidentRepo(incident);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(
            () => _service.CreateSnakebiteIncidentPaymentLinkAsync(
                CreatePaymentRequest(), TestUserId, CancellationToken.None));

        Assert.Contains("already been paid", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Test 3: CreatePaymentLink_WrongOwner_ThrowsForbidden — Req 1.6

    /// <summary>
    /// Validates: Requirement 1.6
    /// WHEN currentUserId != incident.UserId, service SHALL throw ForbiddenException.
    /// </summary>
    [Fact]
    public async Task CreatePaymentLink_WrongOwner_ThrowsForbidden()
    {
        // Arrange
        var otherUserId = Guid.NewGuid();
        var incident = CreateIncident(SnakebiteIncidentStatus.Finished, userId: otherUserId);
        SetupIncidentRepo(incident);

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.CreateSnakebiteIncidentPaymentLinkAsync(
                CreatePaymentRequest(), TestUserId, CancellationToken.None));
    }

    #endregion

    #region Test 4: CreatePaymentLink_GatewayFails_CleansUpPendingTransaction — Req 1.7

    /// <summary>
    /// Validates: Requirement 1.7
    /// IF Payment_Gateway returns error, THEN service SHALL delete pending transaction and throw.
    /// </summary>
    [Fact]
    public async Task CreatePaymentLink_GatewayFails_CleansUpPendingTransaction()
    {
        // Arrange
        var incident = CreateIncident(SnakebiteIncidentStatus.Finished);
        SetupIncidentRepo(incident);

        // No existing pending transaction
        SetupTransactionRepo(null);

        // InsertAsync returns a transaction with an Id
        _transactionRepoMock
            .Setup(r => r.InsertAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction t, CancellationToken _) => t);

        // Gateway fails
        _paymentGatewayMock
            .Setup(g => g.CreatePaymentLinkAsync(It.IsAny<PayOsCreatePaymentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PayOsPaymentLinkResult { Success = false, ErrorMessage = "Gateway error" });

        // After gateway fails, FindIncidentTransactionByOrderCodeAsync is called to find the pending tx
        // We need to set up a sequence: first call returns null (PreparePending check),
        // second call returns a pending tx (cleanup lookup)
        var pendingTx = new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = TestUserId,
            ReferenceId = TestIncidentId,
            TransactionType = TransactionType.SnakebiteIncidentPayment,
            PaymentMethod = "PayOS",
            ExternalTransactionId = null,
            Description = "INCIDENT-123456"
        };

        var transactionCallCount = 0;
        _transactionRepoMock
            .Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<Transaction, bool>>>(),
                It.IsAny<Func<IQueryable<Transaction>, IOrderedQueryable<Transaction>>>(),
                It.IsAny<Func<IQueryable<Transaction>, IQueryable<Transaction>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                transactionCallCount++;
                // First call: PreparePending checks for existing pending tx → null
                // Second call: cleanup after gateway failure → return the pending tx
                return transactionCallCount <= 1 ? null : pendingTx;
            });

        _transactionRepoMock.Setup(r => r.Delete(It.IsAny<Transaction>())).Returns(true);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateSnakebiteIncidentPaymentLinkAsync(
                CreatePaymentRequest(), TestUserId, CancellationToken.None));

        Assert.Contains("Gateway error", ex.Message);
        _transactionRepoMock.Verify(r => r.Delete(It.IsAny<Transaction>()), Times.AtLeastOnce);
    }

    #endregion

    #region Test 5: WalletPayment_InsufficientBalance_ThrowsConflict — Edge case

    /// <summary>
    /// Validates: Edge case — insufficient wallet balance.
    /// WHEN user wallet balance < payment amount, service SHALL throw ConflictException.
    /// </summary>
    [Fact]
    public async Task WalletPayment_InsufficientBalance_ThrowsConflict()
    {
        // Arrange
        var incident = CreateIncident(SnakebiteIncidentStatus.Finished);
        SetupIncidentRepo(incident);

        // User wallet with insufficient balance
        var userWallet = new Wallet { Id = Guid.NewGuid(), UserId = TestUserId, Balance = 50_000m };

        SetupWalletRepoFromList(new[] { userWallet });

        _transactionRepoMock
            .Setup(r => r.InsertAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction t, CancellationToken _) => t);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(
            () => _service.CreateSnakebiteIncidentWalletPaymentAsync(
                CreatePaymentRequest(), TestUserId, CancellationToken.None));

        Assert.Contains("Insufficient wallet balance", ex.Message);
    }

    [Fact]
    public async Task WalletPayment_RecordsSystemRevenueWithoutSystemWallet()
    {
        // Arrange
        var incident = CreateIncident(SnakebiteIncidentStatus.Finished);
        SetupIncidentRepo(incident);

        var userWallet = new Wallet { Id = Guid.NewGuid(), UserId = TestUserId, Balance = 250_000m };
        var insertedWallets = new List<Wallet>();
        var updatedWallets = new List<Wallet>();
        var insertedTransactions = new List<Transaction>();

        SetupWalletRepoFromList(new[] { userWallet });

        _walletRepoMock
            .Setup(r => r.InsertAsync(It.IsAny<Wallet>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Wallet wallet, CancellationToken _) =>
            {
                insertedWallets.Add(wallet);
                return wallet;
            });

        _walletRepoMock
            .Setup(r => r.Update(It.IsAny<Wallet>()))
            .Callback<Wallet>(updatedWallets.Add);

        _transactionRepoMock
            .Setup(r => r.InsertAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction transaction, CancellationToken _) =>
            {
                insertedTransactions.Add(transaction);
                return transaction;
            });

        // Act
        var response = await _service.CreateSnakebiteIncidentWalletPaymentAsync(
            CreatePaymentRequest(), TestUserId, CancellationToken.None);

        // Assert
        Assert.Equal(150_000m, response.UserWalletBalanceAfter);
        Assert.Null(response.SystemWalletBalanceAfter);
        Assert.Equal("Paid", response.Status);
        Assert.Equal(SnakebiteIncidentStatus.Completed, incident.Status);
        Assert.Equal(150_000m, userWallet.Balance);

        Assert.DoesNotContain(insertedWallets, w => w.UserId == SystemWalletUserId);
        Assert.DoesNotContain(updatedWallets, w => w.UserId == SystemWalletUserId);
        Assert.DoesNotContain(insertedTransactions, t => t.TransactionType == TransactionType.EscrowHold);
        Assert.Contains(insertedTransactions, t =>
            t.TransactionType == TransactionType.SnakebiteIncidentPayment
            && t.ReferenceId == TestIncidentId
            && !string.IsNullOrWhiteSpace(t.ExternalTransactionId));
    }

    #endregion

    #region Test 6: Refund_NoOriginalPayment_ThrowsNotFound — Req 4.5

    /// <summary>
    /// Validates: Requirement 4.5
    /// IF no original payment transaction found, THEN service SHALL throw NotFoundException.
    /// </summary>
    [Fact]
    public async Task Refund_NoOriginalPayment_ThrowsNotFound()
    {
        // Arrange
        SetupTransactionRepo(null);

        var request = new RefundTransactionRequest
        {
            ReceiverId = TestUserId,
            ReferenceId = TestIncidentId,
            Amount = 100_000m,
            Description = "Test refund"
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.RefundSnakebiteIncidentTransactionAsync(request, CancellationToken.None));
    }

    #endregion

    #region Test 7: Refund_AmountExceedsOriginal_ThrowsValidation — Req 4.6

    /// <summary>
    /// Validates: Requirement 4.6
    /// WHEN refund amount > original payment amount, service SHALL throw ValidationException.
    /// </summary>
    [Fact]
    public async Task Refund_AmountExceedsOriginal_ThrowsValidation()
    {
        // Arrange
        var originalTx = new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = TestUserId,
            ReferenceId = TestIncidentId,
            Amount = 100_000m,
            TransactionType = TransactionType.SnakebiteIncidentPayment,
            ExternalTransactionId = "PAID-123"
        };
        SetupTransactionRepo(originalTx);

        var request = new RefundTransactionRequest
        {
            ReceiverId = TestUserId,
            ReferenceId = TestIncidentId,
            Amount = 200_000m, // exceeds original
            Description = "Test refund"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _service.RefundSnakebiteIncidentTransactionAsync(request, CancellationToken.None));

        Assert.Contains("cannot exceed", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Refund_UsesSystemRevenueLedgerWithoutSystemWallet()
    {
        // Arrange
        var originalTx = new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = TestUserId,
            ReferenceId = TestIncidentId,
            Amount = 100_000m,
            TransactionType = TransactionType.SnakebiteIncidentPayment,
            PaymentMethod = "Wallet",
            ExternalTransactionId = "PAID-123"
        };

        SetupTransactionRepoFromList(new[] { originalTx });

        var receiverWallet = new Wallet { Id = Guid.NewGuid(), UserId = TestUserId, Balance = 25_000m };
        var updatedWallets = new List<Wallet>();
        var insertedTransactions = new List<Transaction>();
        SetupWalletRepoFromList(new[] { receiverWallet });

        _walletRepoMock
            .Setup(r => r.Update(It.IsAny<Wallet>()))
            .Callback<Wallet>(updatedWallets.Add);

        _transactionRepoMock
            .Setup(r => r.InsertAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction transaction, CancellationToken _) =>
            {
                insertedTransactions.Add(transaction);
                return transaction;
            });

        var request = new RefundTransactionRequest
        {
            ReceiverId = TestUserId,
            ReferenceId = TestIncidentId,
            Amount = 40_000m,
            Description = "Incident refund"
        };

        // Act
        var response = await _service.RefundSnakebiteIncidentTransactionAsync(request, CancellationToken.None);

        // Assert
        Assert.True(response.Success);
        Assert.Null(response.SystemWalletBalanceBefore);
        Assert.Null(response.SystemWalletBalanceAfter);
        Assert.Equal(25_000m, response.ReceiverWalletBalanceBefore);
        Assert.Equal(65_000m, response.ReceiverWalletBalanceAfter);
        Assert.Equal(65_000m, receiverWallet.Balance);

        Assert.DoesNotContain(updatedWallets, w => w.UserId == SystemWalletUserId);
        Assert.DoesNotContain(insertedTransactions, t => t.TransactionType == TransactionType.EscrowRelease);
        Assert.Contains(insertedTransactions, t =>
            t.TransactionType == TransactionType.SnakebiteIncidentRefund
            && t.ReferenceId == TestIncidentId
            && t.Amount == 40_000m);
    }

    #endregion

    #region Test 8: Cancel_AlreadyPaid_PropagatesGatewayError — Req 5.2

    /// <summary>
    /// Validates: Requirement 5.2
    /// WHEN PayOS link is already paid, gateway returns error, service SHALL propagate it.
    /// </summary>
    [Fact]
    public async Task Cancel_AlreadyPaid_PropagatesGatewayError()
    {
        // Arrange
        var pendingTx = new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = TestUserId,
            ReferenceId = TestIncidentId,
            TransactionType = TransactionType.SnakebiteIncidentPayment,
            PaymentMethod = "PayOS",
            ExternalTransactionId = null,
            Description = "INCIDENT-999"
        };
        SetupTransactionRepo(pendingTx);

        _paymentGatewayMock
            .Setup(g => g.CancelPaymentLinkAsync(It.IsAny<long>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PayOsPaymentLinkResult
            {
                Success = false,
                ErrorMessage = "Payment link already paid"
            });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CancelSnakebiteIncidentPaymentLinkAsync(
                999, new CancelPaymentLinkRequest { CancellationReason = "test" }, CancellationToken.None));

        Assert.Contains("Payment link already paid", ex.Message);
    }

    #endregion

    #region Test 9: Confirm_PendingTransaction_VerifiesPayOS — Req 3.4

    /// <summary>
    /// Validates: Requirement 3.4
    /// WHEN ExternalTransactionId is null, service SHALL call GetPaymentLinkInformationAsync.
    /// </summary>
    [Fact]
    public async Task Confirm_PendingTransaction_VerifiesPayOS()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var pendingTx = new Transaction
        {
            Id = transactionId,
            UserId = TestUserId,
            ReferenceId = TestIncidentId,
            Amount = 100_000m,
            TransactionType = TransactionType.SnakebiteIncidentPayment,
            PaymentMethod = "PayOS",
            ExternalTransactionId = null, // not yet confirmed
            Description = "INCIDENT-12345",
            CreatedAt = DateTime.UtcNow
        };
        SetupTransactionRepo(pendingTx);

        // PayOS says not paid yet → ConflictException
        _paymentGatewayMock
            .Setup(g => g.GetPaymentLinkInformationAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PayOsLinkInformation
            {
                Id = "link-1",
                OrderCode = 12345,
                Amount = 100_000,
                AmountPaid = 0,
                AmountRemaining = 100_000,
                Status = "PENDING"
            });

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(
            () => _service.ConfirmSnakebiteIncidentPaymentAsync(transactionId, CancellationToken.None));

        // Verify GetPaymentLinkInformationAsync was called
        _paymentGatewayMock.Verify(
            g => g.GetPaymentLinkInformationAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}
