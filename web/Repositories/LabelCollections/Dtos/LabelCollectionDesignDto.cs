namespace web.Repositories.LabelCollections.Dtos
{
    /// <summary>
    /// Everything the label designer edits and saves in one go ("Gem"): the collection's name, its sheet
    /// grid and all its labels. Sent as JSON (camelCase) both ways; the server validates and normalizes it on save.
    /// </summary>
    public class LabelCollectionDesignDto
    {
        public string Name { get; set; } = string.Empty;
        public int Across { get; set; } = 3;
        public int Down { get; set; } = 8;
        public bool Landscape { get; set; }
        public List<LabelDesignDto> Labels { get; set; } = new();
    }

    /// <summary>One designed label and how many copies of it to print.</summary>
    public class LabelDesignDto
    {
        public int Quantity { get; set; } = 1;
        public string Background { get; set; } = "#ffffff";
        public List<LabelElementDto> Elements { get; set; } = new();
    }

    /// <summary>
    /// One element on a label. Position and size are percentages of the label (0–100), so a design keeps
    /// its layout if the sheet grid changes; list order is the stacking order (last = in front). Only the
    /// fields for the element's Type are used — the rest are cleared on save.
    /// </summary>
    public class LabelElementDto
    {
        /// <summary>Client-side id (for selection in the designer).</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>text, serial, image or qr (LabelDesignRules.Element*).</summary>
        public string Type { get; set; } = "text";

        public double X { get; set; }
        public double Y { get; set; }
        public double W { get; set; }
        public double H { get; set; }

        // text / serial
        public string? Text { get; set; }
        /// <summary>Percentage of the label height. The text is shrunk further if it doesn't fit its box.</summary>
        public double? FontSize { get; set; }
        public bool? Bold { get; set; }
        public bool? Italic { get; set; }
        public string? Align { get; set; }
        public string? VAlign { get; set; }
        public string? Color { get; set; }
        /// <summary>Box background (#rrggbb) — empty means transparent.</summary>
        public string? Background { get; set; }

        // serial: Prefix + the copy's number (Start, Start + 1, ...) padded to Digits + Suffix.
        public int? Start { get; set; }
        public int? Digits { get; set; }
        public string? Prefix { get; set; }
        public string? Suffix { get; set; }

        // image
        public Guid? MediaId { get; set; }

        // qr: Text is the link/text encoded, Color the dark modules.
    }
}
