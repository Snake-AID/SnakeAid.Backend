namespace SnakeAid.Core.Domains
{
    public class PaymentCard : BaseEntity
    {
        public Guid Id { get; set; }
        public string CardNumber { get; set; }
        public string CardHolderName { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string Cvv { get; set; }
        public bool IsDefault { get; set; } = false;
    }
}