using QRCoder;
using SnakeAid.Core.Domains;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace SnakeAid.Service.Implements
{
    public class VietQrAdapter
    {
        public (string Payload, string? ImageBase64) GenerateQr(
            string bankBin,
            string bankAccount,
            string bankName,
            decimal amount,
            string message = "")
        {
            try
            {
                // Build VietQR payload manually (VietQR standard)
                var payload = BuildVietQrPayload(bankBin, bankAccount, amount, message);

                // Generate QR code image using QRCoder
                using var qrGenerator = new QRCodeGenerator();
                using var qrCodeData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new PngByteQRCode(qrCodeData);
                var qrCodeBytes = qrCode.GetGraphic(20);

                // Convert to base64
                var base64Image = Convert.ToBase64String(qrCodeBytes);

                return (payload, base64Image);
            }
            catch (Exception ex)
            {
                // Fallback: return payload without image if QR generation fails
                try
                {
                    var fallbackPayload = BuildVietQrPayload(bankBin, bankAccount, amount, message);
                    return (fallbackPayload, null);
                }
                catch
                {
                    // If even payload building fails, throw original exception
                    throw new InvalidOperationException("Failed to generate VietQR payload", ex);
                }
            }
        }

        private string BuildVietQrPayload(string bankBin, string bankAccount, decimal amount, string message)
        {
            // Basic VietQR payload format
            // This is a simplified implementation - in production, use VietQRHelper library properly
            return $"00020101021138550010A000000727012500069704560113{bankBin}5802VN{bankAccount}5406{amount:F2}5802VN6304XXXX";
        }
    }
}
