using QRCoder;

namespace web.Infrastructure
{
    public static class QrCodeSvg
    {
        /// <summary>An SVG QR code (dark modules in <paramref name="darkColor"/> on white) that scales to its box (viewBox, no fixed size).</summary>
        public static string Create(string text, string darkColor)
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.M);
            return new SvgQRCode(data).GetGraphic(10, darkColor, "#ffffff", true, SvgQRCode.SizingMode.ViewBoxAttribute);
        }
    }
}
