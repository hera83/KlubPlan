namespace web.Data.Entities
{
    /// <summary>
    /// The answer given to a single ArrangementFormField within one ArrangementRegistration.
    /// Mirrors FormAnswer. Checkboxes (multi-select) store their selected options joined with ", ".
    /// </summary>
    public class ArrangementRegistrationAnswer
    {
        public int Id { get; set; }

        public int ArrangementRegistrationId { get; set; }

        public int ArrangementFormFieldId { get; set; }

        public string? ValueText { get; set; }

        public virtual ArrangementRegistration ArrangementRegistration { get; set; } = null!;

        public virtual ArrangementFormField ArrangementFormField { get; set; } = null!;
    }
}
