using QRCoder;

namespace APP.Helpers
{
    // Renders the payslip's "Scan to Verify" QR code server-side, as a PNG
    // data: URI, so the printable payslip page needs no external script or
    // network call (works offline, in a browser print-to-PDF, and in an
    // emailed/attached copy alike). Presentation-layer only - see
    // APP/Models/DTOs/PayslipDto.cs's VerificationUrl/VerificationQrCodeDataUri
    // remarks for why this never lives in the Application/API layer.
    public static class QrCodeHelper
    {
        public static string? GeneratePngDataUri(string? content, int pixelsPerModule = 6)
        {
            if (string.IsNullOrWhiteSpace(content))
                return null;

            try
            {
                using var generator = new QRCodeGenerator();
                using var qrData = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
                var pngQrCode = new PngByteQRCode(qrData);
                var bytes = pngQrCode.GetGraphic(pixelsPerModule);
                return "data:image/png;base64," + Convert.ToBase64String(bytes);
            }
            catch
            {
                // A QR rendering failure must never break the payslip page
                // itself - the view falls back to showing the verification
                // link as plain text when this returns null.
                return null;
            }
        }
    }
}
