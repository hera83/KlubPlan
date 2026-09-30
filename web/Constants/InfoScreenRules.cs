namespace web.Constants
{
    /// <summary>Limits, defaults and allowed values for Værktøjer → Infoskærme (slideshows on projectors/screens).</summary>
    public static class InfoScreenRules
    {
        public const int MaxTitleLength = 200;
        public const int MaxSlides = 100;
        public const int MaxElementsPerSlide = 50;

        public const int MinDurationSeconds = 1;
        public const int MaxDurationSeconds = 3600;
        public const int DefaultDurationSeconds = 10;

        public const int MaxTextLength = 2000;
        public const int MaxQrTextLength = 500;

        /// <summary>Font size as a percentage of the slide height, so text scales with the screen.</summary>
        public const double MinFontSize = 1;
        public const double MaxFontSize = 60;

        public const long MaxImageBytes = 20L * 1024 * 1024;
        public const long MaxVideoBytes = 300L * 1024 * 1024;

        /// <summary>How often a running screen checks for changes (activated/deactivated, saved slides).</summary>
        public const int PollSeconds = 30;

        public const string DefaultBackground = "#111827";

        /// <summary>SVG is left out on purpose — it can carry script and is served from our own origin.</summary>
        public static readonly IReadOnlyDictionary<string, string> ImageTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".png"] = "image/png",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".gif"] = "image/gif",
            [".webp"] = "image/webp"
        };

        public static readonly IReadOnlyDictionary<string, string> VideoTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".mp4"] = "video/mp4",
            [".webm"] = "video/webm"
        };

        public const string MediaKindImage = "image";
        public const string MediaKindVideo = "video";

        /// <summary>Screen formats (width:height) with the Danish UI label.</summary>
        public static readonly IReadOnlyList<(string Key, string Label)> AspectRatios = new[]
        {
            ("16:9", "16:9 (bredformat)"),
            ("16:10", "16:10"),
            ("4:3", "4:3 (ældre projektorer)")
        };
        public const string DefaultAspectRatio = "16:9";

        /// <summary>How one slide changes to the next, with the Danish UI label.</summary>
        public static readonly IReadOnlyList<(string Key, string Label)> Transitions = new[]
        {
            ("fade", "Tone over"),
            ("slide", "Skub fra højre"),
            ("none", "Ingen")
        };
        public const string DefaultTransition = "fade";

        /// <summary>Element types on a slide.</summary>
        public const string ElementText = "text";
        public const string ElementImage = "image";
        public const string ElementVideo = "video";
        public const string ElementQr = "qr";
        public const string ElementClock = "clock";
        public static readonly IReadOnlySet<string> ElementTypes = new HashSet<string> { ElementText, ElementImage, ElementVideo, ElementQr, ElementClock };

        public static readonly IReadOnlySet<string> FontFamilies = new HashSet<string> { "sans", "serif", "mono" };
        public static readonly IReadOnlySet<string> Aligns = new HashSet<string> { "left", "center", "right" };
        public static readonly IReadOnlySet<string> VAligns = new HashSet<string> { "top", "middle", "bottom" };
        public static readonly IReadOnlySet<string> Fits = new HashSet<string> { "contain", "cover" };
        public static readonly IReadOnlySet<string> ClockFormats = new HashSet<string> { "time", "date", "datetime" };
    }
}
