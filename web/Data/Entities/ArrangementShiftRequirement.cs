namespace web.Data.Entities
{
    /// <summary>
    /// A free-text requirement statement for a shift (e.g. "Jeg har kørekort"). Self-declared —
    /// the registrant ticks it to confirm, it is never matched against stored Person fields.
    /// </summary>
    public class ArrangementShiftRequirement
    {
        public int Id { get; set; }

        public int ArrangementShiftId { get; set; }

        public string Text { get; set; } = string.Empty;

        public int Order { get; set; }

        public virtual ArrangementShift Shift { get; set; } = null!;
    }
}
