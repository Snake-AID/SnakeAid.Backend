namespace SnakeAid.Core.Requests.PayOs;

public class CreateSnakeCatchingPaymentRequest
{
    public Guid SnakeCatchingRequestId { get; set; }
    public Guid SenderId { get; set; }  // Customer/Requester
    public Guid ReceiverId { get; set; }  // Catcher/Rescuer
    public decimal Amount { get; set; }
    public string? Description { get; set; }
}
