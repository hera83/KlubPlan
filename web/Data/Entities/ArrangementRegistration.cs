namespace web.Data.Entities
{
    /// <summary>
    /// One completed public sign-up for an Arrangement, submitted via the public /Tilmelding link.
    /// Unlike FormSubmission, the respondent is always a Person — Tilmelding has no anonymous mode,
    /// since the whole point is to know who is signed up for which shift.
    /// </summary>
    public class ArrangementRegistration
    {
        public int Id { get; set; }

        public int ArrangementId { get; set; }

        public int PersonId { get; set; }

        public DateTime RegisteredAtUtc { get; set; } = DateTime.UtcNow;

        public virtual Arrangement Arrangement { get; set; } = null!;

        public virtual Person Person { get; set; } = null!;

        public virtual ICollection<ArrangementRegistrationAnswer> Answers { get; set; } = new List<ArrangementRegistrationAnswer>();

        public virtual ICollection<ArrangementRegistrationShift> Shifts { get; set; } = new List<ArrangementRegistrationShift>();
    }
}
