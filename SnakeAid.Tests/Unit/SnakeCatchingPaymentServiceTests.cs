using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Requests.PayOs;
using SnakeAid.Core.Settings;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Implements;
using SnakeAid.Service.Interfaces;
using SnakeAid.Service.Services.PayOs;
using SnakeAid.Service.Services.PayOs.Models;

namespace SnakeAid.Tests.Unit;

public class SnakeCatchingPaymentServiceTests
{
    private readonly Mock<IUnitOfWork<SnakeAidDbContext>> _unitOfWorkMock;
    private readonly Mock<IPaymentGateway> _paymentGatewayMock;
    private readonly Mock<ILogger<SnakeCatchingPaymentService>> _loggerMock;
    private readonly SnakeCatchingPaymentService _service;

    private readonly Mock<IGenericRepository<SnakeCatchingRequest>> _requestRepoMock;
    private readonly Mock<IGenericRepository<Transaction>> _transactionRepoMock;
    private readonly Mock<IGenericRepository<Wallet>> _walletRepoMock;

    private static readonly Guid TestUserId = Guid.NewGuid();
    private static readonly Guid TestRequestId = Guid.NewGuid();
    private static readonly Guid SystemUserId = Guid.Parse("57288b98-5f91-4de8-b827-866e3df69587");

    public SnakeCatchingPaymentServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork<SnakeAidDbContext>>();
        _paymentGatewayMock = new Mock<IPaymentGateway>();
        _loggerMock = new Mock<ILogger<SnakeCatchingPaymentService>>();

        _requestRepoMock = new Mock<IGenericRepository<SnakeCatchingRequest>>();
        _transactionRepoMock = new Mock<IGenericRepository<Transaction>>();
        _walletRepoMock = new Mock<IGenericRepository<Wallet>>();

        _unitOfWorkMock.Setup(u => u.GetRepository<SnakeCatchingRequest>()).Returns(_requestRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Transaction>()).Returns(_transactionRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Wallet>()).Returns(_walletRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

        _service = new SnakeCatchingPaymentService(
            _paymentGatewayMock.Object,
            _unitOfWorkMock.Object,
            Options.Create(new PayOsOptions()),
            _loggerMock.Object);
    }

    [Fact]
    public async Task CreateWalletPayment_RecordsSystemRevenueWithoutSystemWallet()
    {
        var requestEntity = CreateRequest(RequestStatus.Assigned, TestUserId);
        var userWallet = new Wallet { Id = Guid.NewGuid(), UserId = TestUserId, Balance = 500_000m };
        var insertedTransactions = new List<Transaction>();

        SetupRequestRepo(requestEntity);
        SetupTransactionRepo(existingTransaction: null);
        SetupWalletRepoFromList(new[] { userWallet });
        _transactionRepoMock
            .Setup(r => r.InsertAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction tx, CancellationToken _) =>
            {
                insertedTransactions.Add(tx);
                return tx;
            });

        var response = await _service.CreateWalletPaymentAsync(
            new CreateSnakeCatchingPaymentRequest
            {
                SnakeCatchingRequestId = TestRequestId,
                Amount = 200_000m,
                TransactionType = TransactionType.CatchingPayment
            },
            TestUserId,
            CancellationToken.None);

        Assert.Equal("Paid", response.Status);
        Assert.Single(insertedTransactions);
        Assert.Equal(TransactionType.CatchingPayment, insertedTransactions[0].TransactionType);
        Assert.DoesNotContain(insertedTransactions, t => t.TransactionType == TransactionType.EscrowHold);
        Assert.Equal(300_000m, userWallet.Balance);
        Assert.NotNull(response.GatewayRawResponse);
        Assert.Null(response.GatewayRawResponse!.GetType().GetProperty("SystemWalletBalance"));

        _walletRepoMock.Verify(r => r.Update(It.Is<Wallet>(w => w.UserId == TestUserId)), Times.Once);
        _walletRepoMock.Verify(r => r.Update(It.Is<Wallet>(w => w.UserId == SystemUserId)), Times.Never);
        _walletRepoMock.Verify(r => r.InsertAsync(It.IsAny<Wallet>(), It.IsAny<CancellationToken>()), Times.Never);
        _requestRepoMock.Verify(r => r.Update(It.Is<SnakeCatchingRequest>(req => req.Id == TestRequestId && req.Status == RequestStatus.Completed)), Times.Once);
    }

    [Fact]
    public async Task ConfirmSnakeCatchingPayment_RecordsPayOsRevenueWithoutEscrowHold()
    {
        var pendingTransaction = new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = TestUserId,
            ReferenceId = TestRequestId,
            Amount = 250_000m,
            Currency = "VND",
            TransactionType = TransactionType.CatchingPayment,
            Description = "CATCHING-123456",
            PaymentMethod = "PayOS",
            ExternalTransactionId = null,
            CreatedAt = DateTime.UtcNow
        };
        var requestEntity = CreateRequest(RequestStatus.Assigned, TestUserId);
        var insertedTransactions = new List<Transaction>();

        SetupRequestRepo(requestEntity);
        SetupTransactionRepo(existingTransaction: pendingTransaction, list: new[] { pendingTransaction });
        SetupWalletRepoFromList(Array.Empty<Wallet>());
        _paymentGatewayMock
            .Setup(g => g.GetPaymentLinkInformationAsync(123456, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PayOsLinkInformation
            {
                Id = "plink-1",
                OrderCode = 123456,
                Amount = 250000,
                AmountPaid = 250000,
                Status = "PAID"
            });
        _transactionRepoMock
            .Setup(r => r.InsertAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction tx, CancellationToken _) =>
            {
                insertedTransactions.Add(tx);
                return tx;
            });

        var response = await _service.ConfirmSnakeCatchingPaymentAsync(
            pendingTransaction.Id,
            CancellationToken.None);

        Assert.True(response.Success);
        Assert.Equal(PaymentStatus.Paid, response.Status);
        Assert.Equal("MANUAL-" + pendingTransaction.Id, pendingTransaction.ExternalTransactionId);
        Assert.Empty(insertedTransactions);

        _walletRepoMock.Verify(r => r.Update(It.IsAny<Wallet>()), Times.Never);
        _walletRepoMock.Verify(r => r.InsertAsync(It.IsAny<Wallet>(), It.IsAny<CancellationToken>()), Times.Never);
        _transactionRepoMock.Verify(r => r.InsertAsync(It.Is<Transaction>(t => t.TransactionType == TransactionType.EscrowHold), It.IsAny<CancellationToken>()), Times.Never);
        _requestRepoMock.Verify(r => r.Update(It.Is<SnakeCatchingRequest>(req => req.Id == TestRequestId && req.Status == RequestStatus.Paid)), Times.Once);
    }

    [Fact]
    public async Task TransferToRescuer_ReturnsDeprecatedNoOpWithoutWalletOrTransactionSideEffects()
    {
        var requestEntity = CreateRequest(RequestStatus.Paid, TestUserId);
        requestEntity.AssignedRescuerId = Guid.NewGuid();
        var paidTransactions = new List<Transaction>
        {
            new()
            {
                Id = Guid.NewGuid(),
                UserId = TestUserId,
                ReferenceId = TestRequestId,
                Amount = 350_000m,
                Currency = "VND",
                TransactionType = TransactionType.CatchingPayment,
                PaymentMethod = "Wallet",
                ExternalTransactionId = "WALLET-1",
                CreatedAt = DateTime.UtcNow
            }
        };

        SetupRequestRepo(requestEntity);
        SetupTransactionRepo(existingTransaction: null, list: paidTransactions);
        SetupWalletRepoFromList(Array.Empty<Wallet>());
        _transactionRepoMock
            .Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<Transaction, bool>>>(),
                It.IsAny<Func<IQueryable<Transaction>, IOrderedQueryable<Transaction>>>(),
                It.IsAny<Func<IQueryable<Transaction>, IQueryable<Transaction>>>(),
                It.IsAny<int?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((
                Expression<Func<Transaction, bool>> predicate,
                Func<IQueryable<Transaction>, IOrderedQueryable<Transaction>>? _1,
                Func<IQueryable<Transaction>, IQueryable<Transaction>>? _2,
                int? _3,
                bool _4,
                CancellationToken _5) =>
            {
                var compiled = predicate.Compile();
                return paidTransactions.Where(compiled).ToList();
            });

        var response = await _service.TransferSnakeCatchingFundsToRescuerAsync(
            new SnakeAid.Core.Requests.PayOs.TransferToRescuerRequest
            {
                SnakeCatchingRequestId = TestRequestId
            },
            CancellationToken.None);

        Assert.True(response.Success);
        Assert.Contains("deprecated", response.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(350_000m, response.TotalAmount);
        Assert.Equal(0m, response.NetAmountToRescuer);
        Assert.Null(response.TransferTransactionId);
        Assert.Null(response.SystemWalletBalanceBefore);
        Assert.Null(response.SystemWalletBalanceAfter);
        Assert.Null(response.RescuerWalletBalanceBefore);
        Assert.Null(response.RescuerWalletBalanceAfter);

        _walletRepoMock.Verify(r => r.Update(It.IsAny<Wallet>()), Times.Never);
        _walletRepoMock.Verify(r => r.InsertAsync(It.IsAny<Wallet>(), It.IsAny<CancellationToken>()), Times.Never);
        _transactionRepoMock.Verify(r => r.InsertAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
        _requestRepoMock.Verify(r => r.Update(It.IsAny<SnakeCatchingRequest>()), Times.Never);
    }

    [Fact]
    public async Task RefundSnakeCatchingTransaction_UsesRevenueLedgerWithoutSystemWallet()
    {
        var receiverId = Guid.NewGuid();
        var receiverWallet = new Wallet { Id = Guid.NewGuid(), UserId = receiverId, Balance = 25_000m };
        var transactions = new List<Transaction>
        {
            new()
            {
                Id = Guid.NewGuid(),
                UserId = TestUserId,
                ReferenceId = TestRequestId,
                Amount = 100_000m,
                Currency = "VND",
                TransactionType = TransactionType.CatchingPayment,
                PaymentMethod = "Wallet",
                ExternalTransactionId = "WALLET-1",
                CreatedAt = DateTime.UtcNow
            }
        };
        var insertedTransactions = new List<Transaction>();

        SetupWalletRepoFromList(new[] { receiverWallet });
        SetupTransactionRepo(existingTransaction: null, list: transactions);
        _transactionRepoMock
            .Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<Transaction, bool>>>(),
                It.IsAny<Func<IQueryable<Transaction>, IOrderedQueryable<Transaction>>>(),
                It.IsAny<Func<IQueryable<Transaction>, IQueryable<Transaction>>>(),
                It.IsAny<int?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((
                Expression<Func<Transaction, bool>> predicate,
                Func<IQueryable<Transaction>, IOrderedQueryable<Transaction>>? _1,
                Func<IQueryable<Transaction>, IQueryable<Transaction>>? _2,
                int? _3,
                bool _4,
                CancellationToken _5) =>
            {
                var compiled = predicate.Compile();
                return transactions.Where(compiled).ToList();
            });
        _transactionRepoMock
            .Setup(r => r.InsertAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction tx, CancellationToken _) =>
            {
                insertedTransactions.Add(tx);
                transactions.Add(tx);
                return tx;
            });

        var response = await _service.RefundSnakeCatchingTransactionAsync(
            new RefundTransactionRequest
            {
                ReceiverId = receiverId,
                ReferenceId = TestRequestId,
                Amount = 40_000m,
                Description = "Refund catching payment",
                TransactionType = TransactionType.CatchingRefund
            },
            CancellationToken.None);

        Assert.True(response.Success);
        Assert.Null(response.SystemWalletBalanceBefore);
        Assert.Null(response.SystemWalletBalanceAfter);
        Assert.Equal(25_000m, response.ReceiverWalletBalanceBefore);
        Assert.Equal(65_000m, response.ReceiverWalletBalanceAfter);
        Assert.Contains(insertedTransactions, t => t.TransactionType == TransactionType.CatchingRefund);
        Assert.DoesNotContain(insertedTransactions, t => t.TransactionType == TransactionType.EscrowRelease);

        _walletRepoMock.Verify(r => r.Update(It.Is<Wallet>(w => w.UserId == receiverId && w.Balance == 65_000m)), Times.Once);
    }

    [Fact]
    public async Task RefundSnakeCatchingTransaction_WhenAmountExceedsRefundableRevenue_Throws()
    {
        var receiverId = Guid.NewGuid();
        var receiverWallet = new Wallet { Id = Guid.NewGuid(), UserId = receiverId, Balance = 10_000m };
        var transactions = new List<Transaction>
        {
            new()
            {
                Id = Guid.NewGuid(),
                UserId = TestUserId,
                ReferenceId = TestRequestId,
                Amount = 100_000m,
                Currency = "VND",
                TransactionType = TransactionType.CatchingPayment,
                PaymentMethod = "Wallet",
                ExternalTransactionId = "WALLET-1",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                UserId = receiverId,
                ReferenceId = TestRequestId,
                Amount = 70_000m,
                Currency = "VND",
                TransactionType = TransactionType.CatchingRefund,
                PaymentMethod = "Internal",
                ExternalTransactionId = "REFUND-1",
                CreatedAt = DateTime.UtcNow
            }
        };

        SetupWalletRepoFromList(new[] { receiverWallet });
        SetupTransactionRepo(existingTransaction: null, list: transactions);
        _transactionRepoMock
            .Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<Transaction, bool>>>(),
                It.IsAny<Func<IQueryable<Transaction>, IOrderedQueryable<Transaction>>>(),
                It.IsAny<Func<IQueryable<Transaction>, IQueryable<Transaction>>>(),
                It.IsAny<int?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((
                Expression<Func<Transaction, bool>> predicate,
                Func<IQueryable<Transaction>, IOrderedQueryable<Transaction>>? _1,
                Func<IQueryable<Transaction>, IQueryable<Transaction>>? _2,
                int? _3,
                bool _4,
                CancellationToken _5) =>
            {
                var compiled = predicate.Compile();
                return transactions.Where(compiled).ToList();
            });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.RefundSnakeCatchingTransactionAsync(
                new RefundTransactionRequest
                {
                    ReceiverId = receiverId,
                    ReferenceId = TestRequestId,
                    Amount = 40_000m,
                    Description = "Over refund",
                    TransactionType = TransactionType.CatchingRefund
                },
                CancellationToken.None));

        Assert.Contains("refundable payment amount is insufficient", ex.Message, StringComparison.OrdinalIgnoreCase);
        _walletRepoMock.Verify(r => r.Update(It.IsAny<Wallet>()), Times.Never);
        _transactionRepoMock.Verify(r => r.InsertAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static SnakeCatchingRequest CreateRequest(RequestStatus status, Guid userId)
    {
        return new SnakeCatchingRequest
        {
            Id = TestRequestId,
            UserId = userId,
            Address = "Test",
            AdditionalDetails = "Test",
            Status = status
        };
    }

    private void SetupRequestRepo(SnakeCatchingRequest? request)
    {
        _requestRepoMock
            .Setup(r => r.GetByIdAsync(TestRequestId))
            .ReturnsAsync(request);

        _requestRepoMock
            .Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<SnakeCatchingRequest, bool>>>(),
                It.IsAny<Func<IQueryable<SnakeCatchingRequest>, IOrderedQueryable<SnakeCatchingRequest>>>(),
                It.IsAny<Func<IQueryable<SnakeCatchingRequest>, IQueryable<SnakeCatchingRequest>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);
    }

    private void SetupTransactionRepo(Transaction? existingTransaction, ICollection<Transaction>? list = null)
    {
        _transactionRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid id) => existingTransaction?.Id == id ? existingTransaction : null);

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
                var source = list ?? (existingTransaction != null ? new[] { existingTransaction } : Array.Empty<Transaction>());
                var compiled = predicate.Compile();
                return source.FirstOrDefault(compiled);
            });
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
}
