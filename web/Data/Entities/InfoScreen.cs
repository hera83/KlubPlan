namespace web.Data.Entities
{
    /// <summary>
    /// An info screen (Værktøjer → Infoskærme): a slideshow run by a projector/TV through its public
    /// link /Infoskaerm?Id={PublicId}. Only an active screen plays; the running page picks up
    /// activation and saved changes on its own.
    /// </summary>
    public class InfoScreen
    {
        public int Id { get; set; }

        /// <summary>Unguessable id used in the public link, so screens can't be found by counting up the internal Id.</summary>
        public Guid PublicId { get; set; } = Guid.NewGuid();

        public string Title { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        /// <summary>Screen format, e.g. "16:9" (InfoScreenRules.AspectRatios).</summary>
        public string AspectRatio { get; set; } = "16:9";

        /// <summary>How slides change, e.g. "fade" (InfoScreenRules.Transitions).</summary>
        public string Transition { get; set; } = "fade";

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>Set on every change — the running screen uses it to notice it must reload.</summary>
        public DateTime? UpdatedAtUtc { get; set; }

        public virtual ICollection<InfoScreenSlide> Slides { get; set; } = new List<InfoScreenSlide>();

        public virtual ICollection<InfoScreenMedia> Media { get; set; } = new List<InfoScreenMedia>();
    }
}
