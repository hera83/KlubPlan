namespace web.Constants
{
    /// <summary>
    /// Limits, defaults and allowed values for the label designer (Værktøjer → Labels). The sheet grid,
    /// copies and PDF limits are shared with labels on work list lines and live in <see cref="LabelRules"/>.
    /// </summary>
    public static class LabelDesignRules
    {
        public const int MaxElementsPerLabel = 50;

        public const int MaxTextLength = 1000;
        public const int MaxQrTextLength = 500;
        public const int MaxSerialAffixLength = 50;

        /// <summary>Løbenummer: first number and the number of digits it is padded to with leading zeros (0 = no padding).</summary>
        public const int MaxSerialStart = 999999;
        public const int MaxSerialDigits = 8;

        /// <summary>Font size as a percentage of the label height, so text keeps its size relative to the label if the sheet grid changes.</summary>
        public const double MinFontSize = 1;
        public const double MaxFontSize = 100;
        public const double DefaultFontSize = 16;

        public const long MaxImageBytes = 10L * 1024 * 1024;

        public const string DefaultBackground = "#ffffff";
        public const string DefaultTextColor = "#000000";

        /// <summary>A new collection's sheet: 3 × 8 on portrait A4 (70 × 37 mm) — a common label sheet.</summary>
        public const int DefaultAcross = 3;
        public const int DefaultDown = 8;

        /// <summary>
        /// Images the PDF can embed. SVG is left out on purpose — it can carry script and is served from
        /// our own origin; GIF is left out because only its first frame would be printed anyway.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string> ImageTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".png"] = "image/png",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".webp"] = "image/webp"
        };

        /// <summary>Element types on a label.</summary>
        public const string ElementText = "text";
        public const string ElementSerial = "serial";
        public const string ElementImage = "image";
        public const string ElementQr = "qr";
        public static readonly IReadOnlySet<string> ElementTypes = new HashSet<string> { ElementText, ElementSerial, ElementImage, ElementQr };

        public static readonly IReadOnlySet<string> Aligns = new HashSet<string> { "left", "center", "right" };
        public static readonly IReadOnlySet<string> VAligns = new HashSet<string> { "top", "middle", "bottom" };

        /// <summary>Label size in mm for a sheet grid — A4 split into equal cells with no margin (the same maths as LabelSheetPdf).</summary>
        public static (double Width, double Height) LabelSizeMm(int across, int down, bool landscape)
            => ((landscape ? 297.0 : 210.0) / across, (landscape ? 210.0 : 297.0) / down);
    }
}
