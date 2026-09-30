namespace web.Repositories.InfoScreens.Dtos
{
    /// <summary>
    /// Everything the designer edits and saves in one go ("Gem"): the screen's settings and all its
    /// slides. Sent as JSON (camelCase) both ways; the server validates and normalizes it on save.
    /// </summary>
    public class InfoScreenDesignDto
    {
        public string Title { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string AspectRatio { get; set; } = "16:9";
        public string Transition { get; set; } = "fade";
        public List<InfoScreenSlideDto> Slides { get; set; } = new();
    }

    public class InfoScreenSlideDto
    {
        public int DurationSeconds { get; set; } = 10;
        public string Background { get; set; } = "#111827";
        public bool IsHidden { get; set; }
        public List<InfoScreenElementDto> Elements { get; set; } = new();
    }

    /// <summary>
    /// One element on a slide. Position and size are percentages of the slide (0–100), so a slide
    /// looks the same on every screen size; list order is the stacking order (last = in front).
    /// Only the fields for the element's Type are used — the rest are cleared on save.
    /// </summary>
    public class InfoScreenElementDto
    {
        /// <summary>Client-side id (for selection in the designer).</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>text, image, video, qr or clock (InfoScreenRules.Element*).</summary>
        public string Type { get; set; } = "text";

        public double X { get; set; }
        public double Y { get; set; }
        public double W { get; set; }
        public double H { get; set; }

        // text / clock
        public string? Text { get; set; }
        /// <summary>Percentage of the slide height.</summary>
        public double? FontSize { get; set; }
        public string? FontFamily { get; set; }
        public bool? Bold { get; set; }
        public bool? Italic { get; set; }
        public string? Align { get; set; }
        public string? VAlign { get; set; }
        public string? Color { get; set; }
        /// <summary>Box background (#rrggbb) — empty means transparent.</summary>
        public string? Background { get; set; }
        /// <summary>clock: time, date or datetime.</summary>
        public string? Format { get; set; }

        // image / video
        public Guid? MediaId { get; set; }
        /// <summary>contain (whole picture visible) or cover (fills the box, may crop).</summary>
        public string? Fit { get; set; }

        // qr: Text is the link/text encoded, Color the dark modules.
    }
}
