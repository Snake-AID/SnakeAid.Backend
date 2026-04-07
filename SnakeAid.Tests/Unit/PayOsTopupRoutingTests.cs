using System.Reflection;
using System.Text;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SnakeAid.Api.Controllers;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Requests.PayOs;
using SnakeAid.Core.Responses.Consultation;
using SnakeAid.Core.Responses.PayOs;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Implements;
using SnakeAid.Service.Interfaces;
using SnakeAid.Service.Services.PayOs;
using SnakeAid.Service.Services.PayOs.Models;

namespace SnakeAid.Tests.Unit;

public class PayOsTopupRoutingTests
{
    [Fact]
    public void PayOsController_InjectsWalletTopupService()
    {
        var controllerType = typeof(PayOsController);
        var constructorParams = controllerType.GetConstructors()
            .First()
            .GetParameters()
            .Select(p => p.ParameterType)
            .ToList();

        Assert.Contains(constructorParams, t => t == typeof(IWalletTopupService));
    }

    [Fact]
    public async Task ConfirmPayment_WithTopupPrefix_RoutesToWalletTopupService()
    {
        var topupService = new Mock<IWalletTopupService>();
        topupService.Setup(s => s.ConfirmWalletTopupAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PayOsWebhookResponse
            {
                Success = true,
                Message = "Wallet top-up confirmed successfully"
            });

        var transactionId = Guid.NewGuid();
        await using var db = CreateDbContext();
        await SeedTransactionAsync(db, transactionId, "TOPUP-123456");

        var controller = CreateController(
            topupService.Object,
            new PayOsDescriptionLookup(new UnitOfWork<SnakeAidDbContext>(db)));

        var result = await controller.ConfirmPayment(
            new ConfirmPaymentRequest { TransactionId = transactionId },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        topupService.Verify(s => s.ConfirmWalletTopupAsync(transactionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(PayOsPaymentFlow.Topup, "TOPUP-123456")]
    [InlineData(PayOsPaymentFlow.SnakeCatching, "CATCHING-123456")]
    [InlineData(PayOsPaymentFlow.SnakebiteIncident, "INCIDENT-123456")]
    [InlineData(PayOsPaymentFlow.Consultation, "CONSULTPAY-123456")]
    public async Task ConfirmPayment_RoutesEachPrefixToExpectedOwner(
        PayOsPaymentFlow flow,
        string description)
    {
        var transactionId = Guid.NewGuid();
        var topupService = new Mock<IWalletTopupService>();
        var snakeCatchingService = new Mock<ISnakeCatchingPaymentService>();
        var incidentService = new Mock<ISnakebiteIncidentPaymentService>();
        var consultationService = new Mock<IConsultationPaymentService>();

        topupService.Setup(s => s.ConfirmWalletTopupAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PayOsWebhookResponse { Success = true });
        snakeCatchingService.Setup(s => s.ConfirmSnakeCatchingPaymentAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PayOsWebhookResponse { Success = true });
        incidentService.Setup(s => s.ConfirmSnakebiteIncidentPaymentAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PayOsWebhookResponse { Success = true });
        consultationService.Setup(s => s.ConfirmConsultationPaymentAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConsultationPaymentResponse { TransactionId = transactionId });

        await using var db = CreateDbContext();
        await SeedTransactionAsync(db, transactionId, description);

        var controller = CreateController(
            topupService.Object,
            new PayOsDescriptionLookup(new UnitOfWork<SnakeAidDbContext>(db)),
            snakeCatchingPaymentService: snakeCatchingService.Object,
            snakebiteIncidentPaymentService: incidentService.Object,
            consultationPaymentService: consultationService.Object);

        var result = await controller.ConfirmPayment(
            new ConfirmPaymentRequest { TransactionId = transactionId },
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        VerifyConfirmPaymentOwner(flow, transactionId, topupService, snakeCatchingService, incidentService, consultationService);
    }

    [Fact]
    public async Task Webhook_WithTopupPrefix_RoutesToWalletTopupService()
    {
        var rawPayload = "{\"data\":{}}";
        var topupService = new Mock<IWalletTopupService>();
        topupService.Setup(s => s.ProcessWalletTopupWebhookAsync(rawPayload, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PayOsWebhookResponse
            {
                Success = true,
                Message = "Wallet top-up webhook processed successfully",
                OrderCode = 123456
            });

        var paymentGateway = new Mock<IPaymentGateway>();
        paymentGateway.Setup(g => g.VerifyWebhook(rawPayload))
            .Returns(new PayOsWebhookData
            {
                Success = true,
                Description = "TOPUP-123456",
                OrderCode = 123456
            });

        await using var db = CreateDbContext();
        var controller = CreateController(
            topupService.Object,
            new PayOsDescriptionLookup(new UnitOfWork<SnakeAidDbContext>(db)),
            paymentGateway.Object);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request =
                {
                    Body = new MemoryStream(Encoding.UTF8.GetBytes(rawPayload))
                }
            }
        };

        var result = await controller.Webhook(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        topupService.Verify(s => s.ProcessWalletTopupWebhookAsync(rawPayload, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(PayOsPaymentFlow.Topup, "TOPUP-123456")]
    [InlineData(PayOsPaymentFlow.SnakeCatching, "CATCHING-123456")]
    [InlineData(PayOsPaymentFlow.SnakebiteIncident, "INCIDENT-123456")]
    [InlineData(PayOsPaymentFlow.Consultation, "CONSULTPAY-123456")]
    public async Task Webhook_RoutesEachPrefixToExpectedOwner(
        PayOsPaymentFlow flow,
        string description)
    {
        var rawPayload = "{\"data\":{}}";
        var topupService = new Mock<IWalletTopupService>();
        var snakeCatchingService = new Mock<ISnakeCatchingPaymentService>();
        var incidentService = new Mock<ISnakebiteIncidentPaymentService>();
        var consultationService = new Mock<IConsultationPaymentService>();

        topupService.Setup(s => s.ProcessWalletTopupWebhookAsync(rawPayload, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PayOsWebhookResponse { Success = true });
        snakeCatchingService.Setup(s => s.ProcessSnakeCatchingWebhookAsync(rawPayload, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PayOsWebhookResponse { Success = true });
        incidentService.Setup(s => s.ProcessSnakebiteIncidentWebhookAsync(rawPayload, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PayOsWebhookResponse { Success = true });
        consultationService.Setup(s => s.ProcessConsultationWebhookAsync(rawPayload, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PayOsWebhookResponse { Success = true });

        var paymentGateway = new Mock<IPaymentGateway>();
        paymentGateway.Setup(g => g.VerifyWebhook(rawPayload))
            .Returns(new PayOsWebhookData
            {
                Success = true,
                Description = description,
                OrderCode = 123456
            });

        await using var db = CreateDbContext();
        var controller = CreateController(
            topupService.Object,
            new PayOsDescriptionLookup(new UnitOfWork<SnakeAidDbContext>(db)),
            paymentGateway.Object,
            snakeCatchingService.Object,
            incidentService.Object,
            consultationService.Object);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request =
                {
                    Body = new MemoryStream(Encoding.UTF8.GetBytes(rawPayload))
                }
            }
        };

        var result = await controller.Webhook(CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        VerifyWebhookOwner(flow, rawPayload, topupService, snakeCatchingService, incidentService, consultationService);
    }

    [Fact]
    public async Task ConfirmByOrderCode_WithTopupPrefix_RoutesToWalletTopupService()
    {
        var topupService = new Mock<IWalletTopupService>();
        topupService.Setup(s => s.ConfirmWalletTopupByOrderCodeAsync(123456, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PayOsWebhookResponse { Success = true });

        await using var db = CreateDbContext();
        await SeedTransactionAsync(db, Guid.NewGuid(), "TOPUP-123456");

        var controller = CreateController(
            topupService.Object,
            new PayOsDescriptionLookup(new UnitOfWork<SnakeAidDbContext>(db)));
        var method = typeof(PayOsController).GetMethod("ConfirmByOrderCodeAsync", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(method);

        var task = (Task)method!.Invoke(controller, new object[] { 123456L, CancellationToken.None })!;
        await task;

        topupService.Verify(s => s.ConfirmWalletTopupByOrderCodeAsync(123456, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(PayOsPaymentFlow.Topup, "TOPUP-123456")]
    [InlineData(PayOsPaymentFlow.SnakeCatching, "CATCHING-123456")]
    [InlineData(PayOsPaymentFlow.SnakebiteIncident, "INCIDENT-123456")]
    [InlineData(PayOsPaymentFlow.Consultation, "CONSULTPAY-123456")]
    public async Task ConfirmByOrderCode_RoutesEachPrefixToExpectedOwner(
        PayOsPaymentFlow flow,
        string description)
    {
        const long orderCode = 123456;
        var topupService = new Mock<IWalletTopupService>();
        var snakeCatchingService = new Mock<ISnakeCatchingPaymentService>();
        var incidentService = new Mock<ISnakebiteIncidentPaymentService>();
        var consultationService = new Mock<IConsultationPaymentService>();

        topupService.Setup(s => s.ConfirmWalletTopupByOrderCodeAsync(orderCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PayOsWebhookResponse { Success = true });
        snakeCatchingService.Setup(s => s.ConfirmSnakeCatchingPaymentByOrderCodeAsync(orderCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PayOsWebhookResponse { Success = true });
        incidentService.Setup(s => s.ConfirmSnakebiteIncidentPaymentByOrderCodeAsync(orderCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PayOsWebhookResponse { Success = true });
        consultationService.Setup(s => s.ConfirmConsultationPaymentByOrderCodeAsync(orderCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PayOsWebhookResponse { Success = true });

        await using var db = CreateDbContext();
        await SeedTransactionAsync(db, Guid.NewGuid(), description);

        var controller = CreateController(
            topupService.Object,
            new PayOsDescriptionLookup(new UnitOfWork<SnakeAidDbContext>(db)),
            snakeCatchingPaymentService: snakeCatchingService.Object,
            snakebiteIncidentPaymentService: incidentService.Object,
            consultationPaymentService: consultationService.Object);
        var method = typeof(PayOsController).GetMethod("ConfirmByOrderCodeAsync", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(method);

        var task = (Task)method!.Invoke(controller, new object[] { orderCode, CancellationToken.None })!;
        await task;

        VerifyConfirmByOrderCodeOwner(flow, orderCode, topupService, snakeCatchingService, incidentService, consultationService);
    }

    private static SnakeAidDbContext CreateDbContext()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<SnakeAidDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new TopupRoutingSqliteDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static async Task SeedTransactionAsync(SnakeAidDbContext db, Guid transactionId, string description)
    {
        var userId = Guid.NewGuid();
        db.Set<Account>().Add(new Account
        {
            Id = userId,
            Email = $"topup-{userId:N}@example.com",
            UserName = $"topup-{userId:N}",
            PasswordHash = "hashed",
            FullName = "Topup Tester",
            Role = AccountRole.User,
            IsActive = true
        });

        db.Set<Transaction>().Add(new Transaction
        {
            Id = transactionId,
            UserId = userId,
            ReferenceId = userId,
            Amount = 100000,
            Currency = "VND",
            TransactionType = TransactionType.WalletTopup,
            Description = description,
            PaymentMethod = "PayOS",
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
    }

    private static PayOsController CreateController(
        IWalletTopupService walletTopupService,
        PayOsDescriptionLookup descriptionLookup,
        IPaymentGateway? paymentGateway = null,
        ISnakeCatchingPaymentService? snakeCatchingPaymentService = null,
        ISnakebiteIncidentPaymentService? snakebiteIncidentPaymentService = null,
        IConsultationPaymentService? consultationPaymentService = null)
    {
        return new PayOsController(
            Mock.Of<ILogger<PayOsController>>(),
            new HttpContextAccessor(),
            Mock.Of<IMapper>(),
            walletTopupService,
            snakeCatchingPaymentService ?? Mock.Of<ISnakeCatchingPaymentService>(),
            snakebiteIncidentPaymentService ?? Mock.Of<ISnakebiteIncidentPaymentService>(),
            consultationPaymentService ?? Mock.Of<IConsultationPaymentService>(),
            paymentGateway ?? Mock.Of<IPaymentGateway>(),
            descriptionLookup);
    }

    private static void VerifyConfirmPaymentOwner(
        PayOsPaymentFlow flow,
        Guid transactionId,
        Mock<IWalletTopupService> topupService,
        Mock<ISnakeCatchingPaymentService> snakeCatchingService,
        Mock<ISnakebiteIncidentPaymentService> incidentService,
        Mock<IConsultationPaymentService> consultationService)
    {
        topupService.Verify(s => s.ConfirmWalletTopupAsync(transactionId, It.IsAny<CancellationToken>()), flow == PayOsPaymentFlow.Topup ? Times.Once : Times.Never);
        snakeCatchingService.Verify(s => s.ConfirmSnakeCatchingPaymentAsync(transactionId, It.IsAny<CancellationToken>()), flow == PayOsPaymentFlow.SnakeCatching ? Times.Once : Times.Never);
        incidentService.Verify(s => s.ConfirmSnakebiteIncidentPaymentAsync(transactionId, It.IsAny<CancellationToken>()), flow == PayOsPaymentFlow.SnakebiteIncident ? Times.Once : Times.Never);
        consultationService.Verify(s => s.ConfirmConsultationPaymentAsync(transactionId, It.IsAny<CancellationToken>()), flow == PayOsPaymentFlow.Consultation ? Times.Once : Times.Never);
    }

    private static void VerifyWebhookOwner(
        PayOsPaymentFlow flow,
        string rawPayload,
        Mock<IWalletTopupService> topupService,
        Mock<ISnakeCatchingPaymentService> snakeCatchingService,
        Mock<ISnakebiteIncidentPaymentService> incidentService,
        Mock<IConsultationPaymentService> consultationService)
    {
        topupService.Verify(s => s.ProcessWalletTopupWebhookAsync(rawPayload, It.IsAny<CancellationToken>()), flow == PayOsPaymentFlow.Topup ? Times.Once : Times.Never);
        snakeCatchingService.Verify(s => s.ProcessSnakeCatchingWebhookAsync(rawPayload, It.IsAny<CancellationToken>()), flow == PayOsPaymentFlow.SnakeCatching ? Times.Once : Times.Never);
        incidentService.Verify(s => s.ProcessSnakebiteIncidentWebhookAsync(rawPayload, It.IsAny<CancellationToken>()), flow == PayOsPaymentFlow.SnakebiteIncident ? Times.Once : Times.Never);
        consultationService.Verify(s => s.ProcessConsultationWebhookAsync(rawPayload, It.IsAny<CancellationToken>()), flow == PayOsPaymentFlow.Consultation ? Times.Once : Times.Never);
    }

    private static void VerifyConfirmByOrderCodeOwner(
        PayOsPaymentFlow flow,
        long orderCode,
        Mock<IWalletTopupService> topupService,
        Mock<ISnakeCatchingPaymentService> snakeCatchingService,
        Mock<ISnakebiteIncidentPaymentService> incidentService,
        Mock<IConsultationPaymentService> consultationService)
    {
        topupService.Verify(s => s.ConfirmWalletTopupByOrderCodeAsync(orderCode, It.IsAny<CancellationToken>()), flow == PayOsPaymentFlow.Topup ? Times.Once : Times.Never);
        snakeCatchingService.Verify(s => s.ConfirmSnakeCatchingPaymentByOrderCodeAsync(orderCode, It.IsAny<CancellationToken>()), flow == PayOsPaymentFlow.SnakeCatching ? Times.Once : Times.Never);
        incidentService.Verify(s => s.ConfirmSnakebiteIncidentPaymentByOrderCodeAsync(orderCode, It.IsAny<CancellationToken>()), flow == PayOsPaymentFlow.SnakebiteIncident ? Times.Once : Times.Never);
        consultationService.Verify(s => s.ConfirmConsultationPaymentByOrderCodeAsync(orderCode, It.IsAny<CancellationToken>()), flow == PayOsPaymentFlow.Consultation ? Times.Once : Times.Never);
    }

    private sealed class TopupRoutingSqliteDbContext : SnakeAidDbContext
    {
        public TopupRoutingSqliteDbContext(DbContextOptions<SnakeAidDbContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var keep = new HashSet<Type>
            {
                typeof(Account),
                typeof(Wallet),
                typeof(Transaction)
            };

            var dbSetEntityTypes = typeof(SnakeAidDbContext)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
                .Select(p => p.PropertyType.GetGenericArguments()[0])
                .Distinct();

            foreach (var type in dbSetEntityTypes)
            {
                if (!keep.Contains(type))
                {
                    modelBuilder.Ignore(type);
                }
            }

            modelBuilder.Entity<Account>(entity => entity.HasKey(a => a.Id));
            modelBuilder.Entity<Wallet>(entity =>
            {
                entity.HasKey(w => w.Id);
                entity.HasOne(w => w.Account)
                    .WithMany()
                    .HasForeignKey(w => w.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.HasOne(t => t.User)
                    .WithMany()
                    .HasForeignKey(t => t.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
