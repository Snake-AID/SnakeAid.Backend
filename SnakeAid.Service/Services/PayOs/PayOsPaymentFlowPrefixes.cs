namespace SnakeAid.Service.Services.PayOs;

public enum PayOsPaymentFlow
{
    Topup,
    SnakeCatching,
    SnakebiteIncident,
    Consultation
}

public static class PayOsPaymentFlowPrefixes
{
    public const string Topup = "TOPUP-";
    public const string SnakeCatching = "CATCHING-";
    public const string SnakebiteIncident = "INCIDENT-";
    public const string Consultation = "CONSULTPAY-";

    public static IReadOnlyCollection<string> All { get; } =
    [
        Topup,
        SnakeCatching,
        SnakebiteIncident,
        Consultation
    ];

    public static string GetPrefix(PayOsPaymentFlow flow)
    {
        return flow switch
        {
            PayOsPaymentFlow.Topup => Topup,
            PayOsPaymentFlow.SnakeCatching => SnakeCatching,
            PayOsPaymentFlow.SnakebiteIncident => SnakebiteIncident,
            PayOsPaymentFlow.Consultation => Consultation,
            _ => throw new ArgumentOutOfRangeException(nameof(flow), flow, "Unknown PayOS payment flow.")
        };
    }

    public static string BuildOrderCodePrefix(PayOsPaymentFlow flow, long orderCode)
    {
        return $"{GetPrefix(flow)}{orderCode}";
    }

    public static bool TryResolve(string? description, out PayOsPaymentFlow flow)
    {
        if (description is null)
        {
            flow = default;
            return false;
        }

        if (description.StartsWith(Topup, StringComparison.Ordinal))
        {
            flow = PayOsPaymentFlow.Topup;
            return true;
        }

        if (description.StartsWith(SnakeCatching, StringComparison.Ordinal))
        {
            flow = PayOsPaymentFlow.SnakeCatching;
            return true;
        }

        if (description.StartsWith(SnakebiteIncident, StringComparison.Ordinal))
        {
            flow = PayOsPaymentFlow.SnakebiteIncident;
            return true;
        }

        if (description.StartsWith(Consultation, StringComparison.Ordinal))
        {
            flow = PayOsPaymentFlow.Consultation;
            return true;
        }

        flow = default;
        return false;
    }
}
