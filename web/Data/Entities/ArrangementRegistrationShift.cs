namespace web.Data.Entities
{
    /// <summary>
    /// Join entity: one shift a registrant (or a named companion of theirs, when the arrangement
    /// allows it) signed up for as part of an ArrangementRegistration. One row per person taking
    /// the shift, so capacity counts (against ArrangementShift.NeededCount) are just row counts.
    /// </summary>
    public class ArrangementRegistrationShift
    {
        public int Id { get; set; }

        public int ArrangementRegistrationId { get; set; }

        public int ArrangementShiftId { get; set; }

        /// <summary>Null = the registrant themself. Set = an extra, freely-named companion (e.g. a parent/guardian).</summary>
        public string? CompanionName { get; set; }

        public virtual ArrangementRegistration ArrangementRegistration { get; set; } = null!;

        public virtual ArrangementShift ArrangementShift { get; set; } = null!;
    }
}
