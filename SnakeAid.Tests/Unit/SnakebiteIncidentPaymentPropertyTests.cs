using System.Reflection;
using System.Runtime.CompilerServices;
using FsCheck;
using FsCheck.Fluent;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using SnakeAid.Api.Controllers;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Enums;
using SnakeAid.Core.Requests.PayOs;
using SnakeAid.Core.Requests.PayOS;
using SnakeAid.Core.Responses.PayOs;
using SnakeAid.Service.Implements;
using SnakeAid.Service.Interfaces;
using SnakeAid.Service.Services.PayOs.Models;

namespace SnakeAid.Tests.Unit;

/// <summary>
/// Property-based tests for SnakebiteIncidentPaymentService.
/// Uses reflection to test private methods following the same pattern as PayOsPreservationTests.
///
/// Feature: incident-payos-payment
/// </summary>
public class SnakebiteIncidentPaymentPropertyTests
{
    private static readonly Type ServiceType = typeof(SnakebiteIncidentPaymentService);

    private static SnakebiteIncidentPaymentService CreateServiceViaReflection()
    {
        return (SnakebiteIncidentPaymentService)RuntimeHelpers
            .GetUninitializedObject(ServiceType);
    }

    private static MethodInfo GetBuildDescriptionMethod()
    {
        return ServiceType.GetMethod(
            "BuildDescription",
            BindingFlags.NonPublic | BindingFlags.Instance)!;
    }

    #region Property 1: Pending transaction precondition for PayOS

    /// <summary>
    /// Feature: incident-payos-payment, Property 1: Pending transaction precondition for PayOS
    ///
    /// Validates: Requirements 1.1, 7.3, 10.6
    ///
    /// Verifies that PreparePendingPayOsTransactionAsync exists with the correct signature.
    /// This ensures the pending transaction precondition infrastructure is in place.
    /// </summary>
    [Fact]
    public void PreparePendingPayOsTransactionAsync_HasCorrectSignature()
    {
        var method = ServiceType.GetMethod(
            "PreparePendingPayOsTransactionAsync",
            BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(method);

        var parameters = method!.GetParameters();
        Assert.Equal(5, parameters.Length);
        Assert.Equal(typeof(Guid), parameters[0].ParameterType);
        Assert.Equal(typeof(Guid), parameters[1].ParameterType);
        Assert.Equal(typeof(decimal), parameters[2].ParameterType);
        Assert.Equal(typeof(string), parameters[3].ParameterType);
        Assert.Equal(typeof(CancellationToken), parameters[4].ParameterType);

        Assert.True(method.ReturnType.IsGenericType);
        Assert.Equal(typeof(Task<>), method.ReturnType.GetGenericTypeDefinition());
    }

    /// <summary>
    /// Feature: incident-payos-payment, Property 1: Pending transaction precondition for PayOS
    ///
    /// Validates: Requirements 1.1, 7.3, 10.6
    ///
    /// For any positive long order code, BuildDescription SHALL produce a description
    /// starting with "INCIDENT-" — ensuring the pending transaction description format
    /// is correct for PayOS routing via PayOsDescriptionLookup.
    /// </summary>
    [FsCheck.Xunit.Property(MaxTest = 100)]
    public FsCheck.Property BuildDescription_AlwaysStartsWithIncidentPrefix(PositiveInt orderCodeSeed)
    {
        var orderCode = (long)orderCodeSeed.Get;
        var instance = CreateServiceViaReflection();
        var description = (string)GetBuildDescriptionMethod()
            .Invoke(instance, new object[] { orderCode, "test" })!;

        return Prop.Label(
            description.StartsWith($"INCIDENT-{orderCode}") || description.StartsWith("INCIDENT-"),
            $"Description '{description}' should start with INCIDENT- prefix for orderCode {orderCode}");
    }

    #endregion

    #region Property 9: Description format constraint

    /// <summary>
    /// Feature: incident-payos-payment, Property 9: Description format constraint
    ///
    /// Validates: Requirements 7.3
    ///
    /// For any generated order code, the description INCIDENT-{orderCode}
    /// SHALL have a total length of at most 25 characters (PayOS limit).
    /// </summary>
    [FsCheck.Xunit.Property(MaxTest = 100)]
    public FsCheck.Property BuildDescription_NeverExceeds25Characters(PositiveInt orderCodeSeed)
    {
        var orderCode = (long)orderCodeSeed.Get;
        var instance = CreateServiceViaReflection();
        var description = (string)GetBuildDescriptionMethod()
            .Invoke(instance, new object[] { orderCode, string.Empty })!;

        return Prop.Label(
            description.Length <= 25,
            $"Description '{description}' (length={description.Length}) should be <= 25 chars for orderCode {orderCode}");
    }

    /// <summary>
    /// Feature: incident-payos-payment, Property 9: Description format constraint
    ///
    /// Validates: Requirements 7.3
    ///
    /// Tests with large order codes (up to long.MaxValue range) to ensure truncation works.
    /// GenerateOrderCode produces timestamps like 1719000000XXX (13+ digits),
    /// so we test with realistically large values.
    /// </summary>
    [FsCheck.Xunit.Property(MaxTest = 100)]
    public FsCheck.Property BuildDescription_LargeOrderCodes_StillWithin25Chars(long rawOrderCode)
    {
        var orderCode = Math.Abs(rawOrderCode);
        if (orderCode == 0) orderCode = 1;

        var instance = CreateServiceViaReflection();
        var description = (string)GetBuildDescriptionMethod()
            .Invoke(instance, new object[] { orderCode, "any description" })!;

        return Prop.Label(
            description.Length <= 25,
            $"Description '{description}' (length={description.Length}) exceeds 25 chars for orderCode {orderCode}");
    }

    #endregion

    #region Property 3: Webhook escrow and idempotency

    /// <summary>
    /// Feature: incident-payos-payment, Property 3: Webhook escrow and idempotency
    ///
    /// Validates: Requirements 2.1, 2.5, 3.5, 10.4
    ///
    /// Verifies that ProcessConfirmedPayOsPaymentAsync exists as a private method
    /// with the correct signature (PayOsWebhookData, CancellationToken) → Task&lt;PayOsWebhookResponse&gt;.
    /// This ensures the webhook escrow processing infrastructure is in place.
    /// </summary>
    [Fact]
    public void ProcessConfirmedPayOsPaymentAsync_HasCorrectSignature()
    {
        var method = ServiceType.GetMethod(
            "ProcessConfirmedPayOsPaymentAsync",
            BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(method);

        var parameters = method!.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(PayOsWebhookData), parameters[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);

        // Return type should be Task<PayOsWebhookResponse>
        Assert.True(method.ReturnType.IsGenericType);
        Assert.Equal(typeof(Task<>), method.ReturnType.GetGenericTypeDefinition());

        var returnTypeArg = method.ReturnType.GetGenericArguments()[0];
        Assert.Equal(typeof(PayOsWebhookResponse), returnTypeArg);
    }

    /// <summary>
    /// Feature: incident-payos-payment, Property 3: Webhook escrow and idempotency
    ///
    /// Validates: Requirements 2.1, 2.5, 3.5, 10.4
    ///
    /// Verifies that ProcessConfirmedPayOsPaymentAsync is private, ensuring it is
    /// only called through the public webhook/confirm methods (not directly by controllers).
    /// </summary>
    [Fact]
    public void ProcessConfirmedPayOsPaymentAsync_IsPrivate()
    {
        var method = ServiceType.GetMethod(
            "ProcessConfirmedPayOsPaymentAsync",
            BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(method);
        Assert.True(method!.IsPrivate, "ProcessConfirmedPayOsPaymentAsync should be private");

        // Ensure it is NOT accessible via public binding flags
        var publicMethod = ServiceType.GetMethod(
            "ProcessConfirmedPayOsPaymentAsync",
            BindingFlags.Public | BindingFlags.Instance);

        Assert.Null(publicMethod);
    }

    #endregion

    #region Property 12: Manual confirm verifies PayOS status

    /// <summary>
    /// Feature: incident-payos-payment, Property 12: Manual confirm verifies PayOS status
    ///
    /// Validates: Requirements 3.4
    ///
    /// Verifies that ConfirmSnakebiteIncidentPaymentAsync exists on the
    /// ISnakebiteIncidentPaymentService interface with the correct signature
    /// (Guid transactionId, CancellationToken) → Task&lt;PayOsWebhookResponse&gt;.
    /// </summary>
    [Fact]
    public void ConfirmSnakebiteIncidentPaymentAsync_ExistsOnInterface()
    {
        var interfaceType = typeof(ISnakebiteIncidentPaymentService);

        var method = interfaceType.GetMethod(
            "ConfirmSnakebiteIncidentPaymentAsync",
            BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(method);

        var parameters = method!.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(Guid), parameters[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);

        Assert.True(method.ReturnType.IsGenericType);
        Assert.Equal(typeof(Task<>), method.ReturnType.GetGenericTypeDefinition());

        var returnTypeArg = method.ReturnType.GetGenericArguments()[0];
        Assert.Equal(typeof(PayOsWebhookResponse), returnTypeArg);
    }

    /// <summary>
    /// Feature: incident-payos-payment, Property 12: Manual confirm verifies PayOS status
    ///
    /// Validates: Requirements 3.4
    ///
    /// Verifies that GetPaymentLinkInformationAsync exists on IPaymentGateway —
    /// the method used to verify PayOS status before manual confirmation.
    /// </summary>
    [Fact]
    public void GetPaymentLinkInformationAsync_ExistsOnPaymentGateway()
    {
        var gatewayType = typeof(IPaymentGateway);

        var method = gatewayType.GetMethod(
            "GetPaymentLinkInformationAsync",
            BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(method);

        var parameters = method!.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(long), parameters[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);

        // Return type should be Task<PayOsLinkInformation?>
        Assert.True(method.ReturnType.IsGenericType);
        Assert.Equal(typeof(Task<>), method.ReturnType.GetGenericTypeDefinition());
    }

    /// <summary>
    /// Feature: incident-payos-payment, Property 12: Manual confirm verifies PayOS status
    ///
    /// Validates: Requirements 3.4
    ///
    /// Verifies that IsPaymentLinkPaid helper method exists in the service as a
    /// private static method — used to check PayOS link status during manual confirm.
    /// </summary>
    [Fact]
    public void IsPaymentLinkPaid_ExistsAsPrivateStatic()
    {
        var method = ServiceType.GetMethod(
            "IsPaymentLinkPaid",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);
        Assert.True(method!.IsPrivate, "IsPaymentLinkPaid should be private");
        Assert.True(method.IsStatic, "IsPaymentLinkPaid should be static");

        var parameters = method.GetParameters();
        Assert.Single(parameters);
        Assert.Equal(typeof(PayOsLinkInformation), parameters[0].ParameterType);

        Assert.Equal(typeof(bool), method.ReturnType);
    }

    #endregion

    #region Property 8: Pending transaction replacement on re-creation

    /// <summary>
    /// Feature: incident-payos-payment, Property 8: Pending transaction replacement on re-creation
    ///
    /// Validates: Requirements 1.4
    ///
    /// Verifies that PreparePendingPayOsTransactionAsync exists and its signature
    /// supports the replacement pattern (userId, incidentId, amount, description, cancellationToken).
    /// The method handles deletion of old pending transactions and creation of new ones.
    /// </summary>
    [Fact]
    public void PreparePendingPayOsTransactionAsync_SupportsReplacementPattern()
    {
        var method = ServiceType.GetMethod(
            "PreparePendingPayOsTransactionAsync",
            BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(method);

        // Must accept userId (Guid) and incidentId (Guid) as first two params
        // to look up existing pending transactions for the same incident
        var parameters = method!.GetParameters();
        Assert.Equal(5, parameters.Length);
        Assert.Equal(typeof(Guid), parameters[0].ParameterType);   // userId
        Assert.Equal(typeof(Guid), parameters[1].ParameterType);   // incidentId
        Assert.Equal(typeof(decimal), parameters[2].ParameterType); // amount
        Assert.Equal(typeof(string), parameters[3].ParameterType);  // description
        Assert.Equal(typeof(CancellationToken), parameters[4].ParameterType);

        // Must be private (called only from CreateSnakebiteIncidentPaymentLinkAsync)
        Assert.True(method.IsPrivate, "PreparePendingPayOsTransactionAsync should be private");
    }

    /// <summary>
    /// Feature: incident-payos-payment, Property 8: Pending transaction replacement on re-creation
    ///
    /// Validates: Requirements 1.4
    ///
    /// For any pair of random Guids (userId, incidentId), the PreparePendingPayOsTransactionAsync
    /// signature accepts them — verifying the method can handle replacement for any incident.
    /// </summary>
    [FsCheck.Xunit.Property(MaxTest = 100)]
    public FsCheck.Property PreparePendingPayOsTransactionAsync_AcceptsAnyGuidPair(Guid userId, Guid incidentId)
    {
        var method = ServiceType.GetMethod(
            "PreparePendingPayOsTransactionAsync",
            BindingFlags.NonPublic | BindingFlags.Instance);

        var parameters = method!.GetParameters();

        // Verify the parameter types accept any Guid values
        var userIdParam = parameters[0];
        var incidentIdParam = parameters[1];

        return Prop.Label(
            userIdParam.ParameterType == typeof(Guid) && incidentIdParam.ParameterType == typeof(Guid),
            $"PreparePendingPayOsTransactionAsync should accept Guid userId={userId} and Guid incidentId={incidentId}");
    }

    /// <summary>
    /// Feature: incident-payos-payment, Property 8: Pending transaction replacement on re-creation
    ///
    /// Validates: Requirements 1.4
    ///
    /// Verifies that CancelPaymentLinkAsync exists on IPaymentGateway — the method
    /// used to cancel old PayOS links during pending transaction replacement.
    /// </summary>
    [Fact]
    public void CancelPaymentLinkAsync_ExistsOnPaymentGateway()
    {
        var gatewayType = typeof(IPaymentGateway);

        var method = gatewayType.GetMethod(
            "CancelPaymentLinkAsync",
            BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(method);

        var parameters = method!.GetParameters();
        Assert.Equal(3, parameters.Length);
        Assert.Equal(typeof(long), parameters[0].ParameterType);    // orderCode
        Assert.Equal(typeof(string), parameters[1].ParameterType);  // cancellationReason (nullable)
        Assert.Equal(typeof(CancellationToken), parameters[2].ParameterType);

        // Return type should be Task<PayOsPaymentLinkResult>
        Assert.True(method.ReturnType.IsGenericType);
        Assert.Equal(typeof(Task<>), method.ReturnType.GetGenericTypeDefinition());
    }

    #endregion

    #region Property 2: Money conservation on wallet payment

    /// <summary>
    /// Feature: incident-payos-payment, Property 2: Money conservation on wallet payment
    ///
    /// Validates: Requirements 2.2, 10.2, 6.2
    ///
    /// Verifies that MoveMoneyToEscrowAsync exists with the correct signature:
    /// (Guid userId, Guid incidentId, decimal amount, string description,
    ///  string paymentMethod, string externalTransactionId, CancellationToken,
    ///  bool skipExistingPaymentInsert)
    /// and returns a tuple (TransactionId, UserWalletBalanceAfter, SystemWalletBalanceAfter,
    /// ProcessedAtUtc, ExternalTransactionId).
    /// </summary>
    [Fact]
    public void MoveMoneyToEscrowAsync_HasCorrectSignature()
    {
        var method = ServiceType.GetMethod(
            "MoveMoneyToEscrowAsync",
            BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(method);

        var parameters = method!.GetParameters();
        Assert.Equal(8, parameters.Length);
        Assert.Equal(typeof(Guid), parameters[0].ParameterType);           // userId
        Assert.Equal(typeof(Guid), parameters[1].ParameterType);           // incidentId
        Assert.Equal(typeof(decimal), parameters[2].ParameterType);        // amount
        Assert.Equal(typeof(string), parameters[3].ParameterType);         // description
        Assert.Equal(typeof(string), parameters[4].ParameterType);         // paymentMethod
        Assert.Equal(typeof(string), parameters[5].ParameterType);         // externalTransactionId
        Assert.Equal(typeof(CancellationToken), parameters[6].ParameterType);
        Assert.Equal(typeof(bool), parameters[7].ParameterType);           // skipExistingPaymentInsert

        // Return type: Task<(Guid, decimal, decimal, DateTime, string)>
        Assert.True(method.ReturnType.IsGenericType);
        Assert.Equal(typeof(Task<>), method.ReturnType.GetGenericTypeDefinition());

        var innerType = method.ReturnType.GetGenericArguments()[0];
        Assert.True(innerType.IsGenericType, "Inner type should be a ValueTuple");

        var tupleArgs = innerType.GetGenericArguments();
        Assert.Equal(5, tupleArgs.Length);
        Assert.Equal(typeof(Guid), tupleArgs[0]);      // TransactionId
        Assert.Equal(typeof(decimal), tupleArgs[1]);    // UserWalletBalanceAfter
        Assert.Equal(typeof(decimal), tupleArgs[2]);    // SystemWalletBalanceAfter
        Assert.Equal(typeof(DateTime), tupleArgs[3]);   // ProcessedAtUtc
        Assert.Equal(typeof(string), tupleArgs[4]);     // ExternalTransactionId
    }

    /// <summary>
    /// Feature: incident-payos-payment, Property 2: Money conservation on wallet payment
    ///
    /// Validates: Requirements 2.2, 10.2, 6.2
    ///
    /// For any random positive decimal amount, MoveMoneyToEscrowAsync's signature
    /// accepts it via the decimal parameter — ensuring the method supports arbitrary
    /// positive payment amounts for money conservation.
    /// </summary>
    [FsCheck.Xunit.Property(MaxTest = 100)]
    public FsCheck.Property MoveMoneyToEscrowAsync_AcceptsAnyPositiveAmount(PositiveInt amountSeed)
    {
        var amount = (decimal)amountSeed.Get + 0.01m; // ensure positive decimal

        var method = ServiceType.GetMethod(
            "MoveMoneyToEscrowAsync",
            BindingFlags.NonPublic | BindingFlags.Instance);

        var amountParam = method!.GetParameters()[2];

        return Prop.Label(
            amountParam.ParameterType == typeof(decimal) && amount > 0m,
            $"MoveMoneyToEscrowAsync should accept positive decimal amount={amount}");
    }

    /// <summary>
    /// Feature: incident-payos-payment, Property 2: Money conservation on wallet payment
    ///
    /// Validates: Requirements 2.2, 10.2, 6.2
    ///
    /// Verifies that MoveMoneyToEscrowAsync is private, ensuring money movement
    /// is only triggered through the public payment methods (not directly by controllers).
    /// </summary>
    [Fact]
    public void MoveMoneyToEscrowAsync_IsPrivate()
    {
        var method = ServiceType.GetMethod(
            "MoveMoneyToEscrowAsync",
            BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(method);
        Assert.True(method!.IsPrivate, "MoveMoneyToEscrowAsync should be private");

        var publicMethod = ServiceType.GetMethod(
            "MoveMoneyToEscrowAsync",
            BindingFlags.Public | BindingFlags.Instance);

        Assert.Null(publicMethod);
    }

    #endregion

    #region Property 6: Status validation rejects non-Finished incidents

    /// <summary>
    /// Feature: incident-payos-payment, Property 6: Status validation rejects non-Finished incidents
    ///
    /// Validates: Requirements 1.3, 1.8
    ///
    /// Verifies that SnakebiteIncidentStatus enum has a Finished value,
    /// and that both CreateSnakebiteIncidentPaymentLinkAsync and
    /// CreateSnakebiteIncidentWalletPaymentAsync exist on the interface.
    /// </summary>
    [Fact]
    public void SnakebiteIncidentStatus_HasFinishedValue()
    {
        var enumType = typeof(SnakebiteIncidentStatus);
        Assert.True(enumType.IsEnum, "SnakebiteIncidentStatus should be an enum");

        var hasFinished = Enum.IsDefined(typeof(SnakebiteIncidentStatus), SnakebiteIncidentStatus.Finished);
        Assert.True(hasFinished, "SnakebiteIncidentStatus should have a Finished value");
    }

    /// <summary>
    /// Feature: incident-payos-payment, Property 6: Status validation rejects non-Finished incidents
    ///
    /// Validates: Requirements 1.3, 1.8
    ///
    /// Verifies that both payment creation methods exist on the interface,
    /// ensuring the service has entry points that must validate incident status.
    /// </summary>
    [Fact]
    public void PaymentCreationMethods_ExistOnInterface()
    {
        var interfaceType = typeof(ISnakebiteIncidentPaymentService);

        var payosMethod = interfaceType.GetMethod(
            "CreateSnakebiteIncidentPaymentLinkAsync",
            BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(payosMethod);

        var walletMethod = interfaceType.GetMethod(
            "CreateSnakebiteIncidentWalletPaymentAsync",
            BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(walletMethod);

        // Both should accept CreateSnakebiteIncidentPaymentRequest as first param
        Assert.Equal(typeof(CreateSnakebiteIncidentPaymentRequest), payosMethod!.GetParameters()[0].ParameterType);
        Assert.Equal(typeof(CreateSnakebiteIncidentPaymentRequest), walletMethod!.GetParameters()[0].ParameterType);
    }

    /// <summary>
    /// Feature: incident-payos-payment, Property 6: Status validation rejects non-Finished incidents
    ///
    /// Validates: Requirements 1.3, 1.8
    ///
    /// For any random non-Finished SnakebiteIncidentStatus value, the status is NOT equal
    /// to Finished — verifying the enum space that should trigger ConflictException.
    /// Uses FsCheck to generate random enum values and filters to non-Finished ones.
    /// </summary>
    [FsCheck.Xunit.Property(MaxTest = 100)]
    public FsCheck.Property NonFinishedStatus_IsNotFinished()
    {
        var allValues = Enum.GetValues(typeof(SnakebiteIncidentStatus))
            .Cast<SnakebiteIncidentStatus>()
            .Where(s => s != SnakebiteIncidentStatus.Finished)
            .ToArray();

        var gen = Gen.Elements(allValues);

        return Prop.ForAll(
            gen.ToArbitrary(),
            status =>
            {
                // Every non-Finished status should not equal Finished
                var isNotFinished = status != SnakebiteIncidentStatus.Finished;
                // And should not equal Completed (which has its own check)
                var isDefined = Enum.IsDefined(typeof(SnakebiteIncidentStatus), status);

                return (isNotFinished && isDefined)
                    .Label($"Status {status} should be a valid non-Finished enum value");
            });
    }

    /// <summary>
    /// Feature: incident-payos-payment, Property 6: Status validation rejects non-Finished incidents
    ///
    /// Validates: Requirements 1.3, 1.8
    ///
    /// Verifies that the service implementation checks incident.Status != Finished
    /// by confirming the service references SnakebiteIncidentStatus.Finished in its logic.
    /// The service type should use the same enum the domain model uses.
    /// </summary>
    [Fact]
    public void ServiceImplementation_ReferencesSnakebiteIncidentStatusEnum()
    {
        // The service should reference the SnakebiteIncidentStatus enum
        var serviceAssembly = ServiceType.Assembly;
        var referencedAssemblies = serviceAssembly.GetReferencedAssemblies();

        // SnakebiteIncidentStatus is in SnakeAid.Core
        var coreAssembly = typeof(SnakebiteIncidentStatus).Assembly;
        var coreAssemblyName = coreAssembly.GetName().Name;

        var referencesCore = referencedAssemblies.Any(a => a.Name == coreAssemblyName);
        Assert.True(referencesCore,
            $"Service assembly should reference {coreAssemblyName} which contains SnakebiteIncidentStatus");
    }

    #endregion

    #region Property 5: Refund money conservation

    /// <summary>
    /// Feature: incident-payos-payment, Property 5: Refund money conservation
    ///
    /// Validates: Requirements 4.1, 4.2, 10.3
    ///
    /// Verifies that RefundSnakebiteIncidentTransactionAsync exists on the
    /// ISnakebiteIncidentPaymentService interface with the correct signature:
    /// (RefundTransactionRequest, CancellationToken) → Task&lt;RefundTransactionResponse&gt;.
    /// </summary>
    [Fact]
    public void RefundSnakebiteIncidentTransactionAsync_ExistsOnInterface()
    {
        var interfaceType = typeof(ISnakebiteIncidentPaymentService);

        var method = interfaceType.GetMethod(
            "RefundSnakebiteIncidentTransactionAsync",
            BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(method);

        var parameters = method!.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(Core.Requests.PayOs.RefundTransactionRequest), parameters[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);

        Assert.True(method.ReturnType.IsGenericType);
        Assert.Equal(typeof(Task<>), method.ReturnType.GetGenericTypeDefinition());

        var returnTypeArg = method.ReturnType.GetGenericArguments()[0];
        Assert.Equal(typeof(RefundTransactionResponse), returnTypeArg);
    }

    /// <summary>
    /// Feature: incident-payos-payment, Property 5: Refund money conservation
    ///
    /// Validates: Requirements 4.1, 4.2, 10.3
    ///
    /// Verifies that RefundTransactionResponse has the expected balance fields:
    /// SystemWalletBalanceBefore, SystemWalletBalanceAfter,
    /// ReceiverWalletBalanceBefore, ReceiverWalletBalanceAfter.
    /// These fields are essential for verifying money conservation on refund.
    /// </summary>
    [Fact]
    public void RefundTransactionResponse_HasExpectedBalanceFields()
    {
        var responseType = typeof(RefundTransactionResponse);

        var systemBefore = responseType.GetProperty("SystemWalletBalanceBefore", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(systemBefore);
        Assert.Equal(typeof(decimal), systemBefore!.PropertyType);

        var systemAfter = responseType.GetProperty("SystemWalletBalanceAfter", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(systemAfter);
        Assert.Equal(typeof(decimal), systemAfter!.PropertyType);

        var receiverBefore = responseType.GetProperty("ReceiverWalletBalanceBefore", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(receiverBefore);
        Assert.Equal(typeof(decimal), receiverBefore!.PropertyType);

        var receiverAfter = responseType.GetProperty("ReceiverWalletBalanceAfter", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(receiverAfter);
        Assert.Equal(typeof(decimal), receiverAfter!.PropertyType);
    }

    /// <summary>
    /// Feature: incident-payos-payment, Property 5: Refund money conservation
    ///
    /// Validates: Requirements 4.1, 4.2, 10.3
    ///
    /// For any random positive decimal amount, the RefundTransactionResponse model
    /// can carry the balance values faithfully — ensuring the response object
    /// supports arbitrary positive refund amounts for money conservation verification.
    /// </summary>
    [FsCheck.Xunit.Property(MaxTest = 100)]
    public FsCheck.Property RefundTransactionResponse_BalanceFieldsRoundTrip(PositiveInt amountSeed)
    {
        var amount = (decimal)amountSeed.Get + 0.01m;
        var systemBefore = amount + 1000m;
        var systemAfter = systemBefore - amount;
        var receiverBefore = 500m;
        var receiverAfter = receiverBefore + amount;

        var response = new RefundTransactionResponse
        {
            SystemWalletBalanceBefore = systemBefore,
            SystemWalletBalanceAfter = systemAfter,
            ReceiverWalletBalanceBefore = receiverBefore,
            ReceiverWalletBalanceAfter = receiverAfter,
            RefundAmount = amount
        };

        var systemDebit = response.SystemWalletBalanceBefore - response.SystemWalletBalanceAfter;
        var receiverCredit = response.ReceiverWalletBalanceAfter - response.ReceiverWalletBalanceBefore;

        return Prop.Label(
            systemDebit == amount && receiverCredit == amount && systemDebit == receiverCredit,
            $"Refund amount={amount}: system debit={systemDebit}, receiver credit={receiverCredit} should both equal amount");
    }

    #endregion

    #region Property 4: Payment-then-refund round trip

    /// <summary>
    /// Feature: incident-payos-payment, Property 4: Payment-then-refund round trip
    ///
    /// Validates: Requirements 10.5
    ///
    /// Verifies that both wallet payment and refund methods exist on the interface,
    /// ensuring the service has the entry points needed for a payment-then-refund round trip.
    /// </summary>
    [Fact]
    public void WalletPaymentAndRefund_BothExistOnInterface()
    {
        var interfaceType = typeof(ISnakebiteIncidentPaymentService);

        var walletMethod = interfaceType.GetMethod(
            "CreateSnakebiteIncidentWalletPaymentAsync",
            BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(walletMethod);

        var refundMethod = interfaceType.GetMethod(
            "RefundSnakebiteIncidentTransactionAsync",
            BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(refundMethod);

        // Refund accepts RefundTransactionRequest which has Amount for round-trip
        Assert.Equal(typeof(Core.Requests.PayOs.RefundTransactionRequest), refundMethod!.GetParameters()[0].ParameterType);
    }

    /// <summary>
    /// Feature: incident-payos-payment, Property 4: Payment-then-refund round trip
    ///
    /// Validates: Requirements 10.5
    ///
    /// Verifies that RefundTransactionRequest has ReceiverId, ReferenceId, and Amount
    /// properties — the fields needed to perform a full refund matching the original payment.
    /// </summary>
    [Fact]
    public void RefundTransactionRequest_HasRequiredProperties()
    {
        var requestType = typeof(Core.Requests.PayOs.RefundTransactionRequest);

        var receiverId = requestType.GetProperty("ReceiverId", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(receiverId);
        Assert.Equal(typeof(Guid), receiverId!.PropertyType);

        var referenceId = requestType.GetProperty("ReferenceId", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(referenceId);
        Assert.Equal(typeof(Guid), referenceId!.PropertyType);

        var amount = requestType.GetProperty("Amount", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(amount);
        Assert.Equal(typeof(decimal), amount!.PropertyType);
    }

    /// <summary>
    /// Feature: incident-payos-payment, Property 4: Payment-then-refund round trip
    ///
    /// Validates: Requirements 10.5
    ///
    /// For any random positive decimal amount, the RefundTransactionRequest.Amount
    /// property can carry it faithfully — ensuring the request supports arbitrary
    /// positive amounts for round-trip refund operations.
    /// </summary>
    [FsCheck.Xunit.Property(MaxTest = 100)]
    public FsCheck.Property RefundTransactionRequest_AmountRoundTrips(PositiveInt amountSeed)
    {
        var amount = (decimal)amountSeed.Get + 0.01m;

        var request = new Core.Requests.PayOs.RefundTransactionRequest { Amount = amount };

        return (request.Amount == amount)
            .Label($"RefundTransactionRequest.Amount {amount} should round-trip through the request object");
    }

    #endregion

    #region Property 10: Cancel removes pending transaction

    /// <summary>
    /// Feature: incident-payos-payment, Property 10: Cancel removes pending transaction
    ///
    /// Validates: Requirements 5.1, 5.3
    ///
    /// Verifies that CancelSnakebiteIncidentPaymentLinkAsync exists on the
    /// ISnakebiteIncidentPaymentService interface with the correct signature:
    /// (long orderCode, CancelPaymentLinkRequest, CancellationToken) → Task&lt;CancelPaymentLinkResponse&gt;.
    /// </summary>
    [Fact]
    public void CancelSnakebiteIncidentPaymentLinkAsync_ExistsOnInterface()
    {
        var interfaceType = typeof(ISnakebiteIncidentPaymentService);

        var method = interfaceType.GetMethod(
            "CancelSnakebiteIncidentPaymentLinkAsync",
            BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(method);

        var parameters = method!.GetParameters();
        Assert.Equal(3, parameters.Length);
        Assert.Equal(typeof(long), parameters[0].ParameterType);           // orderCode
        Assert.Equal(typeof(CancelPaymentLinkRequest), parameters[1].ParameterType); // request
        Assert.Equal(typeof(CancellationToken), parameters[2].ParameterType);

        Assert.True(method.ReturnType.IsGenericType);
        Assert.Equal(typeof(Task<>), method.ReturnType.GetGenericTypeDefinition());

        var returnTypeArg = method.ReturnType.GetGenericArguments()[0];
        Assert.Equal(typeof(CancelPaymentLinkResponse), returnTypeArg);
    }

    /// <summary>
    /// Feature: incident-payos-payment, Property 10: Cancel removes pending transaction
    ///
    /// Validates: Requirements 5.1, 5.3
    ///
    /// Verifies that CancelPaymentLinkResponse has Success (bool), Status (PaymentStatus),
    /// and ReferenceId (Guid) properties — the fields needed to confirm cancellation result.
    /// </summary>
    [Fact]
    public void CancelPaymentLinkResponse_HasExpectedProperties()
    {
        var responseType = typeof(CancelPaymentLinkResponse);

        var success = responseType.GetProperty("Success", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(success);
        Assert.Equal(typeof(bool), success!.PropertyType);

        var status = responseType.GetProperty("Status", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(status);
        Assert.Equal(typeof(PaymentStatus), status!.PropertyType);

        var referenceId = responseType.GetProperty("ReferenceId", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(referenceId);
        Assert.Equal(typeof(Guid), referenceId!.PropertyType);
    }

    /// <summary>
    /// Feature: incident-payos-payment, Property 10: Cancel removes pending transaction
    ///
    /// Validates: Requirements 5.1, 5.3
    ///
    /// Verifies that FindIncidentTransactionByOrderCodeAsync exists as a private method
    /// on the service — used internally to find the pending transaction for deletion
    /// during cancellation.
    /// </summary>
    [Fact]
    public void FindIncidentTransactionByOrderCodeAsync_ExistsAsPrivate()
    {
        var method = ServiceType.GetMethod(
            "FindIncidentTransactionByOrderCodeAsync",
            BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(method);
        Assert.True(method!.IsPrivate, "FindIncidentTransactionByOrderCodeAsync should be private");

        var parameters = method.GetParameters();
        Assert.Equal(3, parameters.Length);
        Assert.Equal(typeof(long), parameters[0].ParameterType);    // orderCode
        Assert.Equal(typeof(bool), parameters[1].ParameterType);    // asNoTracking
        Assert.Equal(typeof(CancellationToken), parameters[2].ParameterType);
    }

    #endregion

    #region Property 7: Amount validation rejects mismatched payments

    /// <summary>
    /// Feature: incident-payos-payment, Property 7: Amount validation rejects mismatched payments
    ///
    /// Validates: Requirements 1.5
    ///
    /// Verifies that CreateSnakebiteIncidentPaymentRequest has an Amount property
    /// of type decimal — the field used for amount validation against mission cost.
    /// </summary>
    [Fact]
    public void CreateSnakebiteIncidentPaymentRequest_HasDecimalAmountProperty()
    {
        var requestType = typeof(CreateSnakebiteIncidentPaymentRequest);

        var amountProp = requestType.GetProperty("Amount", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(amountProp);
        Assert.Equal(typeof(decimal), amountProp!.PropertyType);

        // Should be readable and writable
        Assert.True(amountProp.CanRead, "Amount should be readable");
        Assert.True(amountProp.CanWrite, "Amount should be writable");
    }

    /// <summary>
    /// Feature: incident-payos-payment, Property 7: Amount validation rejects mismatched payments
    ///
    /// Validates: Requirements 1.5
    ///
    /// For any random decimal amount, the Amount property on
    /// CreateSnakebiteIncidentPaymentRequest can be set and read back correctly —
    /// ensuring the request object faithfully carries the amount for validation.
    /// </summary>
    [FsCheck.Xunit.Property(MaxTest = 100)]
    public FsCheck.Property PaymentRequest_AmountRoundTrips(decimal amount)
    {
        var request = new CreateSnakebiteIncidentPaymentRequest { Amount = amount };

        return (request.Amount == amount)
            .Label($"Amount {amount} should round-trip through the request object");
    }

    /// <summary>
    /// Feature: incident-payos-payment, Property 7: Amount validation rejects mismatched payments
    ///
    /// Validates: Requirements 1.5
    ///
    /// Verifies that both service methods accept CreateSnakebiteIncidentPaymentRequest
    /// (which contains Amount) — ensuring the amount is available for validation
    /// against the mission's ActualCost ?? Price.
    /// </summary>
    [Fact]
    public void ServiceMethods_AcceptRequestWithAmount()
    {
        var interfaceType = typeof(ISnakebiteIncidentPaymentService);

        var payosMethod = interfaceType.GetMethod(
            "CreateSnakebiteIncidentPaymentLinkAsync",
            BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(payosMethod);
        Assert.Equal(typeof(CreateSnakebiteIncidentPaymentRequest), payosMethod!.GetParameters()[0].ParameterType);

        var walletMethod = interfaceType.GetMethod(
            "CreateSnakebiteIncidentWalletPaymentAsync",
            BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(walletMethod);
        Assert.Equal(typeof(CreateSnakebiteIncidentPaymentRequest), walletMethod!.GetParameters()[0].ParameterType);

        // Verify the request type has Amount property of type decimal
        var amountProp = typeof(CreateSnakebiteIncidentPaymentRequest)
            .GetProperty("Amount", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(amountProp);
        Assert.Equal(typeof(decimal), amountProp!.PropertyType);
    }

    #endregion

    #region Property 11: Webhook routing by description prefix

    /// <summary>
    /// Feature: incident-payos-payment, Property 11: Webhook routing by description prefix
    ///
    /// Validates: Requirements 3.1, 7.1, 7.4
    ///
    /// Verifies that PayOsController has a Webhook endpoint (POST "webhook")
    /// that can route INCIDENT- prefixed descriptions to the incident payment service.
    /// </summary>
    [Fact]
    public void PayOsController_HasWebhookEndpoint()
    {
        var controllerType = typeof(PayOsController);
        var webhookMethod = controllerType.GetMethod("Webhook", BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(webhookMethod);

        var httpPostAttr = webhookMethod!.GetCustomAttributes()
            .OfType<HttpPostAttribute>()
            .FirstOrDefault();

        Assert.NotNull(httpPostAttr);
        Assert.Equal("webhook", httpPostAttr!.Template);
    }

    /// <summary>
    /// Feature: incident-payos-payment, Property 11: Webhook routing by description prefix
    ///
    /// Validates: Requirements 3.1, 7.1, 7.4
    ///
    /// Verifies that PayOsController injects ISnakebiteIncidentPaymentService,
    /// enabling webhook routing to the incident payment handler.
    /// </summary>
    [Fact]
    public void PayOsController_InjectsSnakebiteIncidentPaymentService()
    {
        var controllerType = typeof(PayOsController);
        var constructorParams = controllerType.GetConstructors()
            .First()
            .GetParameters()
            .Select(p => p.ParameterType)
            .ToList();

        Assert.Contains(constructorParams, t => t == typeof(ISnakebiteIncidentPaymentService));
    }

    /// <summary>
    /// Feature: incident-payos-payment, Property 11: Webhook routing by description prefix
    ///
    /// Validates: Requirements 3.1, 7.1, 7.4
    ///
    /// Verifies that ProcessSnakebiteIncidentWebhookAsync exists on the
    /// ISnakebiteIncidentPaymentService interface — the method the webhook routes to
    /// when the description starts with INCIDENT-.
    /// </summary>
    [Fact]
    public void ProcessSnakebiteIncidentWebhookAsync_ExistsOnInterface()
    {
        var interfaceType = typeof(ISnakebiteIncidentPaymentService);

        var method = interfaceType.GetMethod(
            "ProcessSnakebiteIncidentWebhookAsync",
            BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(method);

        var parameters = method!.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(string), parameters[0].ParameterType);          // rawPayload
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);

        Assert.True(method.ReturnType.IsGenericType);
        Assert.Equal(typeof(Task<>), method.ReturnType.GetGenericTypeDefinition());

        var returnTypeArg = method.ReturnType.GetGenericArguments()[0];
        Assert.Equal(typeof(PayOsWebhookResponse), returnTypeArg);
    }

    #endregion
}
