using QRCoder;
using System.Globalization;
using VietQRHelper;

namespace SnakeAid.Service.Implements
{
    public class VietQrAdapter
    {
        private const string VietQrServiceCode = "QRIBFTTA";

        public (string Payload, string? ImageBase64) GenerateQr(
            string bankBin,
            string bankAccount,
            string accountName,
            decimal amount,
            string message = "")
        {
            var payload = BuildVietQrPayload(bankBin, bankAccount, amount, message);

            try
            {
                // Generate QR code image using QRCoder
                using var qrGenerator = new QRCodeGenerator();
                using var qrCodeData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new PngByteQRCode(qrCodeData);
                var qrCodeBytes = qrCode.GetGraphic(20);

                // Convert to base64
                var base64Image = Convert.ToBase64String(qrCodeBytes);

                return (payload, base64Image);
            }
            catch (Exception)
            {
                // Preserve the verified EMV payload even if image rendering fails.
                return (payload, null);
            }
        }

        private string BuildVietQrPayload(string bankBin, string bankAccount, decimal amount, string message)
        {
            if (string.IsNullOrWhiteSpace(bankBin))
            {
                throw new ArgumentException("Bank BIN is required.", nameof(bankBin));
            }

            if (string.IsNullOrWhiteSpace(bankAccount))
            {
                throw new ArgumentException("Bank account is required.", nameof(bankAccount));
            }

            var amountText = amount.ToString("0.##", CultureInfo.InvariantCulture);
            var purpose = message?.Trim() ?? string.Empty;
            var qrPay = QRPay.InitVietQR(bankBin.Trim(), bankAccount.Trim(), amountText, purpose, VietQrServiceCode);
            return qrPay.Build();
        }
    }
}
