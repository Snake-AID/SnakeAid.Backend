namespace SnakeAid.Core.Responses.Wallet
{
    public class BankDirectoryResponse
    {
        public string Bin { get; set; }
        public string Name { get; set; }
        public VietQrStatus VietQrStatus { get; set; }
    }

    public enum VietQrStatus
    {
        TransferSupported,
        ReceiveOnly,
        NotSupported
    }
}