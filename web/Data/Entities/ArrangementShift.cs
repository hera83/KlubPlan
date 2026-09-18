namespace web.Data.Entities
{
    /// <summary>
    /// A shift ("vagt") within an Arrangement — a concrete time slot with a headcount need.
    /// Requirements ("krav") are scoped to the shift, not the arrangement, since e.g. "kørekort"
    /// is naturally tied to which shift needs it.
    /// </summary>
    public class ArrangementShift
    {
        public int Id { get; set; }

        public int ArrangementId { get; set; }

        public string Title { get; set; } = string.Empty;

        public DateTime StartUtc { get; set; }

        public DateTime EndUtc { get; set; }

        public string? Location { get; set; }

        /// <summary>How many people are needed for this shift.</summary>
        public int NeededCount { get; set; } = 1;

        /// <summary>Tie-breaker display order for shifts with the same StartUtc (0-based).</summary>
        public int Order { get; set; }

        public virtual Arrangement Arrangement { get; set; } = null!;

        public virtual ICollection<ArrangementShiftRequirement> Requirements { get; set; } = new List<ArrangementShiftRequirement>();

        public virtual ICollection<ArrangementRegistrationShift> Registrations { get; set; } = new List<ArrangementRegistrationShift>();
    }
}
