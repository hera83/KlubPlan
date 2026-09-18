namespace web.Data.Entities
{
    /// <summary>Join entity: one shift a registrant signed up for as part of an ArrangementRegistration.</summary>
    public class ArrangementRegistrationShift
    {
        public int ArrangementRegistrationId { get; set; }

        public int ArrangementShiftId { get; set; }

        public virtual ArrangementRegistration ArrangementRegistration { get; set; } = null!;

        public virtual ArrangementShift ArrangementShift { get; set; } = null!;
    }
}
