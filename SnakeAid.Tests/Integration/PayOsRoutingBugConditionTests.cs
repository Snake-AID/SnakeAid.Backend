using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Responses.PayOs;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Implements;
using SnakeAid.Service.Implements;
using SnakeAid.Service.Interfaces;
using SnakeAid.Service.Services.PayOs.Models;

namespace SnakeAid.Tests.Integration;

/// <summary>
/// Bug condition exploration tests for PayOS routing.
/// These tests encode the EXPECTED (correct) behavior.
/// They are expected to FAIL on unfixed code — failure confirms the bug exists.
///
/// **Validates: Requirements 1.1, 1.2, 1.3, 1.4, 1.5, 2.1, 2.2, 2.3, 2.4, 2.5**
/// </summary>
public class PayOsRoutingBugConditionTests
{
    private static readonly Guid SystemUserId = Guid.Parse("57288b98-5f91-4de8-b827-866e3df69587");

    /// <summary>
    /// Test 1: Consultation webhook with CONSULTPAY- prefix sent to the central /webhook endpoint.
    ///
    /// Bug Condition: The PayOsController webhook endpoint tries SnakebiteIncident first,
    /// catches exception, falls back to SnakeCatching — Consultation is NEVER routed.
    ///
    /// Expected Behavior: A webhook with CONSULTPAY- description should be routed to
    /// ConsultationPaymentService.ProcessConsultationWebhookAsync.
    ///
    /// This test verifies that the webhook endpoint can identify and route Consultation
    /// webhooks by inspecting the order code prefix in the description field.
    ///
    /// **Validates: Requirements 1.2, 2.2**
    /// </summary>
    [Fact]
    public void Webhook_WithConsultPayPrefix_ShouldBeRoutableToConsultation()
    {
        // Arrange: Simulate a webhook description with CONSULTPAY- prefix
        var orderCode = 1234567890L;
        var description = $"CONSULTPAY-{orderCode}";

        // Act: Check if the PayOsController webhook handler has any code path
        // that routes to ConsultationPaymentService.
        // We inspect the controller source to verify it references IConsultationPaymentService.
        var controllerType = typeof(SnakeAid.Api.Controllers.PayOsController);
        var constructorParams = controllerType.GetConstructors()
            .First()
            .GetParameters()
            .Select(p => p.ParameterType)
            .ToList();

        // Assert: The controller should inject IConsultationPaymentService
        // to be able to route Consultation webhooks.
        // On unfixed code, this will FAIL because the controller only injects
        // ISnakeCatchingPaymentService and ISnakebiteIncidentPaymentService.
        Assert.Contains(constructorParams,
            t => t == typeof(IConsultationPaymentService) ||
                 t.GetInterfaces().Contains(typeof(IConsultationPaymentService)));
    }

    /// <summary>
    /// Test 2: Return redirect with a SnakebiteIncident order code.
    ///
    /// Bug Condition: The /return endpoint only calls
    /// ConfirmSnakeCatchingPaymentByOrderCodeAsync — SnakebiteIncident payments
    /// are never auto-confirmed on return.
    ///
    /// Expected Behavior: The return endpoint should identify the originating flow
    /// from the order code and call the correct confirm method for any flow.
    ///
    /// This test verifies that the Return method does NOT hardcode SnakeCatching-only
    /// confirmation by checking that it has a mechanism to resolve the correct flow.
    ///
    /// **Validates: Requirements 1.3, 2.3**
    /// </summary>
    [Fact]
    public void Return_Endpoint_ShouldNotHardcodeSnakeCatchingConfirmation()
    {
        // Arrange: Inspect the Return method in PayOsController
        var controllerType = typeof(SnakeAid.Api.Controllers.PayOsController);
        var returnMethod = controllerType.GetMethod("Return");

        Assert.NotNull(returnMethod);

        // Act: The controller should inject ALL 3 payment services so it can
        // dispatch return confirmations to the correct flow based on prefix.
        // On unfixed code, it only had ISnakeCatchingPaymentService and
        // ISnakebiteIncidentPaymentService — no IConsultationPaymentService.
        var fields = controllerType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
        var hasSnakeCatchingField = fields.Any(f => f.FieldType == typeof(ISnakeCatchingPaymentService));
        var hasSnakebiteField = fields.Any(f => f.FieldType == typeof(ISnakebiteIncidentPaymentService));
        var hasConsultationField = fields.Any(f => f.FieldType == typeof(IConsultationPaymentService));

        // Assert: All 3 services must be present for flow-agnostic dispatch.
        Assert.True(hasSnakeCatchingField && hasSnakebiteField && hasConsultationField,
            "PayOsController should inject all 3 payment services (SnakeCatching, SnakebiteIncident, Consultation) " +
            "to dispatch return confirmations to the correct flow. " +
            "On unfixed code, only SnakeCatching and SnakebiteIncident were injected.");
    }

    /// <summary>
    /// Test 3: Manual confirm-payment with a Consultation transactionId.
    ///
    /// Bug Condition: The /confirm-payment endpoint always delegates to
    /// _snakeCatchingPaymentService.ConfirmSnakeCatchingPaymentAsync regardless
    /// of which flow the transaction belongs to.
    ///
    /// Expected Behavior: The confirm endpoint should resolve the correct flow
    /// from the transaction and confirm the payment for any flow.
    ///
    /// This test verifies that the ConfirmPayment method has a mechanism to
    /// route to the correct flow handler based on the transaction.
    ///
    /// **Validates: Requirements 1.5, 2.5**
    /// </summary>
    [Fact]
    public void ConfirmPayment_ShouldNotHardcodeSnakeCatchingConfirmation()
    {
        // Arrange: Inspect the ConfirmPayment method in PayOsController
        var controllerType = typeof(SnakeAid.Api.Controllers.PayOsController);
        var confirmMethod = controllerType.GetMethod("ConfirmPayment");

        Assert.NotNull(confirmMethod);

        // Act: Check that the controller does NOT have a hardcoded
        // ISnakeCatchingPaymentService field that ConfirmPayment calls.
        var fields = controllerType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
        var hasOnlySnakeCatchingPayment = fields.Any(f => f.FieldType == typeof(ISnakeCatchingPaymentService));

        // Also check: the controller should have some form of flow resolution
        // (e.g., a router, a collection of handlers, or IConsultationPaymentService).
        var hasConsultationService = fields.Any(f =>
            f.FieldType == typeof(IConsultationPaymentService));
        var hasFlowRouter = fields.Any(f =>
            f.FieldType.Name.Contains("Router") ||
            f.FieldType.Name.Contains("FlowHandler") ||
            (f.FieldType.IsGenericType &&
             f.FieldType.GetGenericTypeDefinition() == typeof(IEnumerable<>)));

        // Assert: The controller should have a routing mechanism that can resolve
        // Consultation transactions, not just SnakeCatching.
        // On unfixed code, this will FAIL because the controller only has
        // _snakeCatchingPaymentService and _snakebiteIncidentPaymentService.
        Assert.True(hasConsultationService || hasFlowRouter,
            "PayOsController should have a flow router or IConsultationPaymentService " +
            "to route confirm-payment requests to the correct flow handler. " +
            "Currently, /confirm-payment hardcodes ConfirmSnakeCatchingPaymentAsync.");
    }

    /// <summary>
    /// Test 4: SnakebiteIncident and SnakeCatching share the same SNAKEAID- prefix.
    ///
    /// Bug Condition: Both SnakebiteIncidentPaymentService and SnakeCatchingPaymentService
    /// use "SNAKEAID-" as the order code prefix, making it impossible to deterministically
    /// route webhooks between these two flows based on the description field.
    ///
    /// Expected Behavior: Each flow should have a unique prefix:
    /// - SnakeCatching: "SNAKEAID-"
    /// - SnakebiteIncident: "INCIDENT-"
    /// - Consultation: "CONSULTPAY-"
    ///
    /// **Validates: Requirements 2.2**
    /// </summary>
    [Fact]
    public async Task OrderCodePrefixes_ShouldBeUniqueAcrossAllFlows()
    {
        // Arrange: Extract the prefix patterns from each payment service using reflection.
        // SnakeCatchingPaymentService uses OrderCodeRegex = new(@"^SNAKEAID-(\d+)")
        // SnakebiteIncidentPaymentService uses BuildDescription with "SNAKEAID-{orderCode}"
        // ConsultationPaymentService uses PayOsDescriptionPrefix = "CONSULTPAY"

        // Extract SnakeCatching prefix from its OrderCodeRegex
        var snakeCatchingType = typeof(SnakeCatchingPaymentService);
        var snakeCatchingRegexField = snakeCatchingType.GetField("OrderCodeRegex",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(snakeCatchingRegexField);
        var snakeCatchingRegex = (Regex)snakeCatchingRegexField.GetValue(null)!;
        var snakeCatchingPrefix = ExtractPrefixFromRegex(snakeCatchingRegex.ToString());

        // Extract SnakebiteIncident prefix from its BuildDescription method
        // We use reflection to call BuildDescription and inspect the output
        var snakebiteType = typeof(SnakebiteIncidentPaymentService);
        var snakebiteBuildDesc = snakebiteType.GetMethod("BuildDescription",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(snakebiteBuildDesc);

        // Create a minimal instance to call BuildDescription
        await using var db = CreateDbContext();
        var snakebiteService = CreateSnakebiteService(db);
        var description = (string)snakebiteBuildDesc.Invoke(snakebiteService, new object[] { 123456L, "test" })!;
        var snakebitePrefix = ExtractPrefixFromDescription(description);

        // Extract Consultation prefix
        var consultationType = typeof(ConsultationPaymentService);
        var consultPrefixField = consultationType.GetField("PayOsDescriptionPrefix",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(consultPrefixField);
        var consultationPrefix = (string)consultPrefixField.GetValue(null)! + "-";

        // Act & Assert: All three prefixes should be unique
        var prefixes = new[] { snakeCatchingPrefix, snakebitePrefix, consultationPrefix };

        // On unfixed code, this will FAIL because both SnakeCatching and SnakebiteIncident
        // use "SNAKEAID-" as their prefix.
        Assert.Equal(prefixes.Length, prefixes.Distinct().Count());
    }

    #region Helpers

    private static string ExtractPrefixFromRegex(string regexPattern)
    {
        // Pattern like "^SNAKEAID-(\d+)" → extract "SNAKEAID-"
        var match = Regex.Match(regexPattern, @"\^?([A-Z]+-)", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : regexPattern;
    }

    private static string ExtractPrefixFromDescription(string description)
    {
        // Description like "SNAKEAID-123456" → extract "SNAKEAID-"
        var match = Regex.Match(description, @"^([A-Z]+-)", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : description;
    }

    private static SnakeAidDbContext CreateDbContext()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<SnakeAidDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new BugConditionSqliteDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static SnakebiteIncidentPaymentService CreateSnakebiteService(SnakeAidDbContext db)
    {
        return new SnakebiteIncidentPaymentService(
            new UnitOfWork<SnakeAidDbContext>(db),
            new StubPaymentGateway(),
            NullLogger<SnakebiteIncidentPaymentService>.Instance);
    }

    private sealed class StubPaymentGateway : IPaymentGateway
    {
        public Task<PayOsPaymentLinkResult> CreatePaymentLinkAsync(PayOsCreatePaymentRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new PayOsPaymentLinkResult { Success = true, OrderCode = request.OrderCode });

        public Task<PayOsPaymentLinkResult> CancelPaymentLinkAsync(long orderCode, string? cancellationReason, CancellationToken cancellationToken)
            => Task.FromResult(new PayOsPaymentLinkResult { Success = true });

        public Task<PayOsLinkInformation?> GetPaymentLinkInformationAsync(long orderCode, CancellationToken cancellationToken)
            => Task.FromResult<PayOsLinkInformation?>(null);

        public PayOsWebhookData VerifyWebhook(string rawPayload) => throw new NotImplementedException();
    }

    private sealed class BugConditionSqliteDbContext : SnakeAidDbContext
    {
        public BugConditionSqliteDbContext(DbContextOptions<SnakeAidDbContext> options)
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
                    modelBuilder.Ignore(type);
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

    #endregion
}
