namespace web.Data.Entities
{
    /// <summary>One page ("side") in an InfoScreen's slideshow.</summary>
    public class InfoScreenSlide
    {
        public int Id { get; set; }

        public int InfoScreenId { get; set; }

        /// <summary>Position in the slideshow.</summary>
        public int Order { get; set; }

        /// <summary>How long the slide is shown before the next one.</summary>
        public int DurationSeconds { get; set; } = 10;

        /// <summary>Background color (#rrggbb).</summary>
        public string Background { get; set; } = "#111827";

        /// <summary>A hidden slide is kept in the designer but skipped when the screen plays.</summary>
        public bool IsHidden { get; set; }

        /// <summary>
        /// The slide's elements (text, image, video, QR code, clock) with position and style, as JSON
        /// (Repositories/InfoScreens/Dtos/InfoScreenElementDto). Validated and normalized on save; the
        /// designer and the player render it with wwwroot/js/infoscreen-render.js.
        /// </summary>
        public string ElementsJson { get; set; } = "[]";

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAtUtc { get; set; }

        public virtual InfoScreen Screen { get; set; } = null!;
    }
}
