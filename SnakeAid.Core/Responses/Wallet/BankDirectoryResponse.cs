namespace SnakeAid.Core.Responses.Wallet
{
    public class BankDirectoryResponse
    {
        public string? Key { get; set; }
        public string? Code { get; set; }
        public string? ShortName { get; set; }
        public string Bin { get; set; }
        public string Name { get; set; }
        public VietQrStatus VietQrStatus { get; set; }
        public bool LookupSupported { get; set; }
        public string? SwiftCode { get; set; }
    }

    public enum VietQrStatus
    {
        TransferSupported,
        ReceiveOnly,
        NotSupported
    }
}
