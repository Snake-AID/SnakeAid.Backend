using SnakeAid.Core.Domains;
using SnakeAid.Core.Requests.PayOs;
using SnakeAid.Core.Responses.PayOs;

namespace SnakeAid.Core.Mappings;

public static class PaymentContextMapper
{
    public static PaymentContext ToPaymentContext(this CreateSnakeCatchingPaymentRequest request, Guid currentUserId)
    {
        return new PaymentContext
        {
            ReferenceId = request.SnakeCatchingRequestId,
            ReferenceType = PaymentReferenceType.SnakeCatching,
            Amount = request.Amount,
            SenderId = currentUserId,
            ReceiverId = Guid.Parse("57288b98-5f91-4de8-b827-866e3df69587"), // System account
            Description = $"Snake catching payment - {request.SnakeCatchingRequestId}",
            ItemName = "Snake Catching Service",
            Quantity = 1,
            Metadata = new Dictionary<string, object>
            {
                ["TransactionType"] = request.TransactionType.ToString()
            }
        };
    }

    public static PaymentResult ToPaymentResult(this SnakeCatchingPaymentResponse response)
    {
        return new PaymentResult
        {
            ReferenceId = response.SnakeCatchingRequestId,
            ReferenceType = PaymentReferenceType.SnakeCatching,
            TransactionId = response.TransactionId,
            Amount = response.Amount,
            Status = response.Status,
            CheckoutUrl = response.CheckoutUrl,
            OrderCode = response.OrderCode,
            PaymentLinkId = response.PaymentLinkId,
            ExpiresAt = response.ExpiresAt,
            Provider = response.Provider,
            GatewayRawResponse = response.GatewayRawResponse,
            Success = true
        };
    }
}