using SnakeAid.Service.Services.PayOs;

namespace SnakeAid.Tests.Unit;

public class PayOsPaymentFlowPrefixesTests
{
    [Fact]
    public void AllPrefixes_AreUnique()
    {
        var prefixes = PayOsPaymentFlowPrefixes.All.ToArray();

        Assert.Equal(prefixes.Length, prefixes.Distinct(StringComparer.Ordinal).Count());
    }

    [Theory]
    [InlineData(PayOsPaymentFlow.Topup, "TOPUP-")]
    [InlineData(PayOsPaymentFlow.SnakeCatching, "CATCHING-")]
    [InlineData(PayOsPaymentFlow.SnakebiteIncident, "INCIDENT-")]
    [InlineData(PayOsPaymentFlow.Consultation, "CONSULTPAY-")]
    public void GetPrefix_ReturnsExpectedPrefix(PayOsPaymentFlow flow, string expectedPrefix)
    {
        Assert.Equal(expectedPrefix, PayOsPaymentFlowPrefixes.GetPrefix(flow));
    }

    [Theory]
    [InlineData("TOPUP-123456", PayOsPaymentFlow.Topup)]
    [InlineData("CATCHING-123456 - service", PayOsPaymentFlow.SnakeCatching)]
    [InlineData("INCIDENT-123456", PayOsPaymentFlow.SnakebiteIncident)]
    [InlineData("CONSULTPAY-123456", PayOsPaymentFlow.Consultation)]
    public void TryResolve_ResolvesFlowFromDescriptionPrefix(string description, PayOsPaymentFlow expectedFlow)
    {
        var resolved = PayOsPaymentFlowPrefixes.TryResolve(description, out var flow);

        Assert.True(resolved);
        Assert.Equal(expectedFlow, flow);
    }

    [Fact]
    public void TryResolve_ReturnsFalseForUnknownDescription()
    {
        var resolved = PayOsPaymentFlowPrefixes.TryResolve("SNAKEAID-123456", out var flow);

        Assert.False(resolved);
        Assert.Equal(default, flow);
    }

    [Theory]
    [InlineData(PayOsPaymentFlow.Topup, 123456L, "TOPUP-123456")]
    [InlineData(PayOsPaymentFlow.SnakeCatching, 123456L, "CATCHING-123456")]
    [InlineData(PayOsPaymentFlow.SnakebiteIncident, 123456L, "INCIDENT-123456")]
    [InlineData(PayOsPaymentFlow.Consultation, 123456L, "CONSULTPAY-123456")]
    public void BuildOrderCodePrefix_ReturnsPrefixWithOrderCode(
        PayOsPaymentFlow flow,
        long orderCode,
        string expectedPrefix)
    {
        Assert.Equal(expectedPrefix, PayOsPaymentFlowPrefixes.BuildOrderCodePrefix(flow, orderCode));
    }
}
