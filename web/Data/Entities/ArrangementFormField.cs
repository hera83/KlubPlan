using web.Constants;

namespace web.Data.Entities
{
    /// <summary>
    /// A single field in an Arrangement's own signup form. Deliberately decoupled from
    /// Form/FormField/FormSubmission — the eventual public sign-up flow identifies the
    /// respondent by Person (no login), so it cannot reuse FormSubmission.SubmittedByUserId.
    /// Reuses FormFieldType since "kind of form field" is a generic concept.
    /// </summary>
    public class ArrangementFormField
    {
        public int Id { get; set; }

        public int ArrangementId { get; set; }

        public string Label { get; set; } = string.Empty;

        public string? HelpText { get; set; }

        public FormFieldType FieldType { get; set; }

        public bool IsRequired { get; set; }

        /// <summary>Display order within the arrangement's signup form (0-based).</summary>
        public int Order { get; set; }

        /// <summary>
        /// JSON array of option strings. Only populated for field types where
        /// FormFieldTypes.HasOptions(FieldType) is true (Dropdown/MultipleChoice/Checkboxes).
        /// </summary>
        public string? OptionsJson { get; set; }

        public virtual Arrangement Arrangement { get; set; } = null!;
    }
}
