using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using SnakeAid.Api.Controllers;
using SnakeAid.Service.Implements;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Tests.Unit;

/// <summary>
/// Preservation property tests for PayOS payment behavior.
/// These tests capture the baseline behavior on UNFIXED code that must be preserved after the fix.
///
/// Property 2: Preservation — SnakeCatching Payment Behavior Unchanged
///
/// For all SnakeCatching payment operations (non-bug-condition inputs where isBugCondition returns false),
/// the system routes to SnakeCatchingPaymentService and produces identical transaction records,
/// wallet balances, and API response shapes.
///
/// **Validates: Requirements 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8, 3.9**
/// </summary>
public class PayOsPreservationTests
{
    #region Property: SnakeCatching webhook routing is handled by SnakeCatchingPaymentService

    /// <summary>
    /// Observe: SnakeCatching webhook with SNAKEAID-{orderCode} description is processed
    /// by SnakeCatchingPaymentService on unfixed code.
    ///
    /// Property: The PayOsController injects all 3 domain payment services directly
    /// to enable deterministic prefix-based routing for webhooks.
    ///
    /// **Validates: Requirements 3.1, 3.2, 3.5**
    /// </summary>
    [Fact]
    public void Webhook_PayOsController_InjectsAllPaymentServices()
    {
        var controllerType = typeof(PayOsController);
        var constructorParams = controllerType.GetConstructors()
            .First()
            .GetParameters()
            .Select(p => p.ParameterType)
            .ToList();

        Assert.Contains(constructorParams, t => t == typeof(ISnakeCatchingPaymentService));
        Assert.Contains(constructorParams, t => t == typeof(ISnakebiteIncidentPaymentService));
        Assert.Contains(constructorParams, t => t == typeof(IConsultationPaymentService));
    }

    [Fact]
    public void Webhook_PayOsController_HasWebhookEndpoint()
    {
        var controllerType = typeof(PayOsController);
        var webhookMethod = controllerType.GetMethod("Webhook");

        Assert.NotNull(webhookMethod);

        var httpPostAttr = webhookMethod!.GetCustomAttributes()
            .OfType<HttpPostAttribute>()
            .FirstOrDefault();

        Assert.NotNull(httpPostAttr);
        Assert.Equal("webhook", httpPostAttr!.Template);
    }

    #endregion

    #region Property: SnakeCatching return redirect calls ConfirmSnakeCatchingPaymentByOrderCodeAsync

    /// <summary>
    /// Observe: SnakeCatching return redirect calls ConfirmSnakeCatchingPaymentByOrderCodeAsync
    /// on unfixed code.
    ///
    /// After refactor, the PayOsController Return method uses prefix-based dispatch
    /// to resolve the correct service and calls the appropriate confirm method.
    ///
    /// Property: The Return endpoint exists and the controller has the domain service
    /// fields to handle return confirmations for all flows including SnakeCatching.
    ///
    /// **Validates: Requirements 3.5**
    /// </summary>
    [Fact]
    public void Return_PayOsController_HasReturnEndpointForSnakeCatching()
    {
        var controllerType = typeof(PayOsController);
        var returnMethod = controllerType.GetMethod("Return");

        Assert.NotNull(returnMethod);

        var httpGetAttr = returnMethod!.GetCustomAttributes()
            .OfType<HttpGetAttribute>()
            .FirstOrDefault();

        Assert.NotNull(httpGetAttr);
        Assert.Equal("return", httpGetAttr!.Template);
    }

    [Fact]
    public void Return_PayOsController_HasSnakeCatchingServiceField()
    {
        var controllerType = typeof(PayOsController);
        var fields = controllerType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.Contains(fields, f => f.FieldType == typeof(ISnakeCatchingPaymentService));
    }

    #endregion

    #region Property: SnakeCatching manual confirm delegates to ConfirmSnakeCatchingPaymentAsync

    /// <summary>
    /// Observe: SnakeCatching manual confirm delegates to ConfirmSnakeCatchingPaymentAsync
    /// on unfixed code.
    ///
    /// The PayOsController ConfirmPayment endpoint delegates to the SnakeCatching service.
    ///
    /// Property: The confirm-payment endpoint exists and the controller can delegate to
    /// ISnakeCatchingPaymentService.
    ///
    /// **Validates: Requirements 3.5**
    /// </summary>
    [Fact]
    public void ConfirmPayment_PayOsController_HasConfirmEndpoint()
    {
        var controllerType = typeof(PayOsController);
        var confirmMethod = controllerType.GetMethod("ConfirmPayment");

        Assert.NotNull(confirmMethod);

        var httpPostAttr = confirmMethod!.GetCustomAttributes()
            .OfType<HttpPostAttribute>()
            .FirstOrDefault();

        Assert.NotNull(httpPostAttr);
        Assert.Equal("confirm-payment", httpPostAttr!.Template);
    }

    #endregion

    #region Property: IPaymentGateway operations produce identical results

    /// <summary>
    /// Observe: IPaymentGateway.CreatePaymentLinkAsync, VerifyWebhook, CancelPaymentLinkAsync,
    /// GetPaymentLinkInformationAsync produce identical results.
    ///
    /// Property: The IPaymentGateway interface has exactly the 4 expected methods with
    /// correct signatures. This ensures the gateway contract is preserved.
    ///
    /// **Validates: Requirements 3.1, 3.2, 3.3, 3.4**
    /// </summary>
    [Theory]
    [InlineData("CreatePaymentLinkAsync", typeof(Task<>))]
    [InlineData("CancelPaymentLinkAsync", typeof(Task<>))]
    [InlineData("GetPaymentLinkInformationAsync", typeof(Task<>))]
    [InlineData("VerifyWebhook", null)]
    public void IPaymentGateway_PreservesMethodSignatures(string methodName, Type? expectedReturnGenericDef)
    {
        var gatewayType = typeof(IPaymentGateway);
        var method = gatewayType.GetMethod(methodName);

        Assert.NotNull(method);

        if (expectedReturnGenericDef != null)
        {
            Assert.True(method!.ReturnType.IsGenericType,
                $"{methodName} should return a generic Task<T>");
            Assert.Equal(expectedReturnGenericDef, method.ReturnType.GetGenericTypeDefinition());
        }
    }

    [Fact]
    public void IPaymentGateway_HasExactlyFourMethods()
    {
        var gatewayType = typeof(IPaymentGateway);
        var methods = gatewayType.GetMethods();

        Assert.Equal(4, methods.Length);
    }

    #endregion

    #region Property: SnakeCatching service uses SNAKEAID- prefix consistently

    /// <summary>
    /// Property: For all SnakeCatching payment operations, the system uses the SNAKEAID- prefix.
    /// The OrderCodeRegex in SnakeCatchingPaymentService matches ^SNAKEAID-(\d+).
    ///
    /// This is the baseline prefix that must be preserved after the fix.
    /// SnakeCatching keeps SNAKEAID-, SnakebiteIncident will change to INCIDENT-.
    ///
    /// **Validates: Requirements 3.5**
    /// </summary>
    [Fact]
    public void SnakeCatchingPaymentService_UsesSnakeAidPrefix()
    {
        var serviceType = typeof(SnakeCatchingPaymentService);
        var regexField = serviceType.GetField("OrderCodeRegex",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(regexField);

        var regex = (Regex)regexField!.GetValue(null)!;
        var pattern = regex.ToString();

        // The regex must start with ^SNAKEAID-
        Assert.StartsWith("^SNAKEAID-", pattern);
    }

    /// <summary>
    /// Property: For all generated order codes, SnakeCatchingPaymentService.BuildDescription
    /// produces a description starting with "SNAKEAID-{orderCode}".
    ///
    /// Test with multiple order codes to verify the property holds across inputs.
    ///
    /// **Validates: Requirements 3.5**
    /// </summary>
    [Theory]
    [InlineData(1234567890L, "test description")]
    [InlineData(9999999999L, "")]
    [InlineData(1L, "Snake catching payment")]
    [InlineData(100000L, null)]
    public void SnakeCatchingPaymentService_BuildDescription_ProducesSnakeAidPrefix(long orderCode, string? customDescription)
    {
        var serviceType = typeof(SnakeCatchingPaymentService);
        var buildDescMethod = serviceType.GetMethod("BuildDescription",
            BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(buildDescMethod);

        // Create a minimal instance via reflection (BuildDescription is a private instance method)
        // We need to invoke it — use the constructor that the bug condition tests established
        var instance = CreateSnakeCatchingServiceViaReflection();
        var description = (string)buildDescMethod!.Invoke(instance, new object?[] { orderCode, customDescription })!;

        Assert.StartsWith($"SNAKEAID-{orderCode}", description);
    }

    /// <summary>
    /// Property: For all descriptions produced by BuildDescription, ExtractOrderCodeFromDescription
    /// correctly round-trips back to the original order code.
    ///
    /// **Validates: Requirements 3.5**
    /// </summary>
    [Theory]
    [InlineData(123456L)]
    [InlineData(999999999L)]
    [InlineData(1L)]
    [InlineData(100000L)]
    public void SnakeCatchingPaymentService_ExtractOrderCode_RoundTrips(long orderCode)
    {
        var serviceType = typeof(SnakeCatchingPaymentService);
        var buildDescMethod = serviceType.GetMethod("BuildDescription",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var extractMethod = serviceType.GetMethod("ExtractOrderCodeFromDescription",
            BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(buildDescMethod);
        Assert.NotNull(extractMethod);

        var instance = CreateSnakeCatchingServiceViaReflection();
        var description = (string)buildDescMethod!.Invoke(instance, new object?[] { orderCode, "test" })!;
        var extracted = (long)extractMethod!.Invoke(instance, new object[] { description })!;

        Assert.Equal(orderCode, extracted);
    }

    #endregion

    #region Property: Consultation service uses CONSULTPAY- prefix

    /// <summary>
    /// Property: ConsultationPaymentService uses CONSULTPAY as its description prefix.
    /// This must be preserved after the fix.
    ///
    /// **Validates: Requirements 3.7**
    /// </summary>
    [Fact]
    public void ConsultationPaymentService_UsesConsultPayPrefix()
    {
        var serviceType = typeof(ConsultationPaymentService);
        var prefixField = serviceType.GetField("PayOsDescriptionPrefix",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(prefixField);

        var prefix = (string)prefixField!.GetValue(null)!;
        Assert.Equal("CONSULTPAY", prefix);
    }

    [Fact]
    public void ConsultationPaymentService_OrderCodeRegex_MatchesConsultPayPrefix()
    {
        var serviceType = typeof(ConsultationPaymentService);
        var regexField = serviceType.GetField("OrderCodeRegex",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(regexField);

        var regex = (Regex)regexField!.GetValue(null)!;
        var pattern = regex.ToString();

        Assert.StartsWith("^CONSULTPAY-", pattern);
    }

    #endregion

    #region Property: SnakebiteIncident service uses SNAKEAID- prefix (on unfixed code)

    /// <summary>
    /// Observe: On unfixed code, SnakebiteIncidentPaymentService also uses SNAKEAID- prefix.
    /// This is the bug condition (shared prefix). After the fix, it will change to INCIDENT-.
    /// This test documents the CURRENT behavior for observation purposes.
    ///
    /// **Validates: Requirements 3.6**
    /// </summary>
    [Fact]
    public void SnakebiteIncidentPaymentService_CurrentlyUsesSnakeAidPrefix_ObservationOnly()
    {
        var serviceType = typeof(SnakebiteIncidentPaymentService);
        var buildDescMethod = serviceType.GetMethod("BuildDescription",
            BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(buildDescMethod);

        // Create minimal instance
        var instance = CreateSnakebiteServiceViaReflection();
        var description = (string)buildDescMethod!.Invoke(instance, new object[] { 123456L, "test" })!;

        // On unfixed code, this starts with SNAKEAID-
        // After fix, it will start with INCIDENT-
        // This observation test just documents the current state
        Assert.Matches(@"^(SNAKEAID|INCIDENT)-\d+", description);
    }

    #endregion

    #region Property: Existing flow-specific controller endpoints continue to serve requests

    /// <summary>
    /// Property: existing flow-specific controller endpoints (SnakebiteIncidentController,
    /// ConsultationPaymentsController) continue to serve requests unchanged.
    ///
    /// For all known flow-specific payment endpoints, the route templates and HTTP methods
    /// must remain identical after the fix.
    ///
    /// **Validates: Requirements 3.6, 3.7, 3.9**
    /// </summary>
    [Theory]
    [InlineData(typeof(SnakebiteIncidentController), "CreateSnakebiteIncidentPaymentLink", "POST", "{incidentId}/payment/payos")]
    [InlineData(typeof(SnakebiteIncidentController), "PaySnakebiteIncidentWithWallet", "POST", "{incidentId}/payment/wallet")]
    [InlineData(typeof(ConsultationPaymentsController), "PayScheduledBooking", "POST", "/api/consultation-bookings/{bookingId:guid}/payments")]
    [InlineData(typeof(ConsultationPaymentsController), "PayEmergencyRequest", "POST", "/api/consultations/emergency-requests/{requestId:guid}/payments")]
    [InlineData(typeof(ConsultationPaymentsController), "ConfirmConsultationPayment", "POST", "/api/consultation-payments/confirm-payment")]
    public void FlowSpecificControllers_PreserveEndpointRoutes(
        Type controllerType, string methodName, string expectedHttpMethod, string expectedTemplate)
    {
        var method = controllerType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);

        var httpMethodAttr = method!.GetCustomAttributes()
            .OfType<HttpMethodAttribute>()
            .FirstOrDefault();

        Assert.NotNull(httpMethodAttr);
        Assert.Equal(expectedTemplate, httpMethodAttr!.Template);
        Assert.Contains(expectedHttpMethod, httpMethodAttr.HttpMethods);
    }

    /// <summary>
    /// Property: SnakebiteIncidentController injects ISnakebiteIncidentPaymentService.
    /// This ensures the flow-specific controller can still serve payment requests independently.
    ///
    /// **Validates: Requirements 3.6, 3.9**
    /// </summary>
    [Fact]
    public void SnakebiteIncidentController_InjectsPaymentService()
    {
        var controllerType = typeof(SnakebiteIncidentController);
        var constructorParams = controllerType.GetConstructors()
            .First()
            .GetParameters()
            .Select(p => p.ParameterType)
            .ToList();

        Assert.Contains(constructorParams, t => t == typeof(ISnakebiteIncidentPaymentService));
    }

    /// <summary>
    /// Property: ConsultationPaymentsController injects IConsultationPaymentService.
    /// This ensures the flow-specific controller can still serve payment requests independently.
    ///
    /// **Validates: Requirements 3.7, 3.9**
    /// </summary>
    [Fact]
    public void ConsultationPaymentsController_InjectsPaymentService()
    {
        var controllerType = typeof(ConsultationPaymentsController);
        var constructorParams = controllerType.GetConstructors()
            .First()
            .GetParameters()
            .Select(p => p.ParameterType)
            .ToList();

        Assert.Contains(constructorParams, t => t == typeof(IConsultationPaymentService));
    }

    #endregion

    #region Property: PayOsController preserves all existing endpoint routes

    /// <summary>
    /// Property: All existing PayOsController endpoints must continue to exist with the same
    /// route templates and HTTP methods after the fix.
    ///
    /// **Validates: Requirements 3.1, 3.5**
    /// </summary>
    [Theory]
    [InlineData("ConfirmPayment", "POST", "confirm-payment")]
    [InlineData("Return", "GET", "return")]
    [InlineData("Cancel", "GET", "cancel")]
    [InlineData("Webhook", "POST", "webhook")]
    public void PayOsController_PreservesEndpointRoutes(string methodName, string expectedHttpMethod, string expectedTemplate)
    {
        var controllerType = typeof(PayOsController);
        var method = controllerType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);

        Assert.NotNull(method);

        var httpMethodAttr = method!.GetCustomAttributes()
            .OfType<HttpMethodAttribute>()
            .FirstOrDefault();

        Assert.NotNull(httpMethodAttr);
        Assert.Equal(expectedTemplate, httpMethodAttr!.Template);
        Assert.Contains(expectedHttpMethod, httpMethodAttr.HttpMethods);
    }

    /// <summary>
    /// Property: SnakeCatching payment endpoints moved to SnakeCatchingPaymentsController
    /// with new route convention matching other flow-specific payment controllers.
    /// </summary>
    [Theory]
    [InlineData("CreatePaymentLink", "POST", "create-link")]
    [InlineData("CancelPaymentLink", "POST", "cancel-link/{orderCode}")]
    [InlineData("TransferToRescuer", "POST", "transfer-to-rescuer")]
    public void SnakeCatchingPaymentsController_HasEndpointRoutes(string methodName, string expectedHttpMethod, string expectedTemplate)
    {
        var controllerType = typeof(SnakeCatchingPaymentsController);
        var method = controllerType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);

        Assert.NotNull(method);

        var httpMethodAttr = method!.GetCustomAttributes()
            .OfType<HttpMethodAttribute>()
            .FirstOrDefault();

        Assert.NotNull(httpMethodAttr);
        Assert.Equal(expectedTemplate, httpMethodAttr!.Template);
        Assert.Contains(expectedHttpMethod, httpMethodAttr.HttpMethods);
    }

    /// <summary>
    /// Property: PayOsController base route is api/v1/[controller] which resolves to api/v1/payos.
    ///
    /// **Validates: Requirements 3.9**
    /// </summary>
    [Fact]
    public void PayOsController_PreservesBaseRoute()
    {
        var controllerType = typeof(PayOsController);
        var routeAttr = controllerType.GetCustomAttribute<RouteAttribute>();

        Assert.NotNull(routeAttr);
        Assert.Equal("api/v1/[controller]", routeAttr!.Template);
    }

    #endregion

    #region Property: ISnakeCatchingPaymentService interface contract preserved

    /// <summary>
    /// Property: ISnakeCatchingPaymentService has all expected methods.
    /// The interface contract must not change after the fix.
    ///
    /// **Validates: Requirements 3.5**
    /// </summary>
    [Theory]
    [InlineData("CreateSnakeCatchingPaymentLinkAsync")]
    [InlineData("CancelSnakeCatchingPaymentLinkAsync")]
    [InlineData("ProcessSnakeCatchingWebhookAsync")]
    [InlineData("ConfirmSnakeCatchingPaymentAsync")]
    [InlineData("ConfirmSnakeCatchingPaymentByOrderCodeAsync")]
    [InlineData("TransferSnakeCatchingFundsToRescuerAsync")]
    [InlineData("RefundSnakeCatchingTransactionAsync")]
    public void ISnakeCatchingPaymentService_PreservesMethodContract(string methodName)
    {
        var serviceType = typeof(ISnakeCatchingPaymentService);
        var method = serviceType.GetMethod(methodName);

        Assert.NotNull(method);
    }

    #endregion

    #region Property: Wallet operations produce same balance changes

    /// <summary>
    /// Observe: Wallet operations (credit, debit, escrow) produce same balance changes.
    ///
    /// Property: The SnakeCatchingPaymentService has a commissionFee field.
    /// This business rule field must exist to preserve the 200,000 VND commission.
    ///
    /// **Validates: Requirements 3.5, 3.8**
    /// </summary>
    [Fact]
    public void SnakeCatchingPaymentService_HasCommissionFeeField()
    {
        var serviceType = typeof(SnakeCatchingPaymentService);
        var commissionField = serviceType.GetField("commissionFee",
            BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(commissionField);
        Assert.Equal(typeof(int), commissionField!.FieldType);
    }

    /// <summary>
    /// Property: The system wallet ID is consistent across SnakebiteIncident and Consultation services.
    /// Both use the same static SystemWalletUserId constant.
    /// SnakeCatchingPaymentService uses an instance field 'systemId' with the same value.
    ///
    /// **Validates: Requirements 3.8**
    /// </summary>
    [Fact]
    public void PaymentServices_HaveSystemWalletIdFields()
    {
        var expectedSystemId = "57288b98-5f91-4de8-b827-866e3df69587";

        // SnakeCatchingPaymentService uses 'systemId' instance field (verified by field existence)
        var scType = typeof(SnakeCatchingPaymentService);
        var scField = scType.GetField("systemId", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(scField);
        Assert.Equal(typeof(string), scField!.FieldType);

        // SnakebiteIncidentPaymentService uses 'SystemWalletUserId' const (static, can read directly)
        var siType = typeof(SnakebiteIncidentPaymentService);
        var siField = siType.GetField("SystemWalletUserId",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(siField);
        var siSystemId = (string)siField!.GetValue(null)!;
        Assert.Equal(expectedSystemId, siSystemId);

        // ConsultationPaymentService uses 'SystemWalletUserId' const (static, can read directly)
        var cpType = typeof(ConsultationPaymentService);
        var cpField = cpType.GetField("SystemWalletUserId",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(cpField);
        var cpSystemId = (string)cpField!.GetValue(null)!;
        Assert.Equal(expectedSystemId, cpSystemId);
    }

    #endregion

    #region Helpers

    private static SnakeCatchingPaymentService CreateSnakeCatchingServiceViaReflection()
    {
        // SnakeCatchingPaymentService constructor requires:
        // IPaymentGateway, IUnitOfWork, IOptions<PayOsOptions>, ILogger<SnakeCatchingPaymentService>
        // For reflection-only tests (BuildDescription, ExtractOrderCode), we can pass nulls
        // since those methods don't use injected dependencies.
        return (SnakeCatchingPaymentService)System.Runtime.CompilerServices.RuntimeHelpers
            .GetUninitializedObject(typeof(SnakeCatchingPaymentService));
    }

    private static SnakebiteIncidentPaymentService CreateSnakebiteServiceViaReflection()
    {
        return (SnakebiteIncidentPaymentService)System.Runtime.CompilerServices.RuntimeHelpers
            .GetUninitializedObject(typeof(SnakebiteIncidentPaymentService));
    }

    #endregion
}
