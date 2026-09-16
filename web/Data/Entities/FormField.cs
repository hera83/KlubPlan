using web.Constants;

namespace web.Data.Entities
{
    /// <summary>
    /// A single field/question in a Form's definition.
    /// </summary>
    public class FormField
    {
        public int Id { get; set; }

        public int FormId { get; set; }

        public string Label { get; set; } = string.Empty;

        public string? HelpText { get; set; }

        public FormFieldType FieldType { get; set; }

        public bool IsRequired { get; set; }

        /// <summary>Display order within the form (0-based).</summary>
        public int Order { get; set; }

        /// <summary>
        /// JSON array of option strings. Only populated for field types where
        /// FormFieldTypes.HasOptions(FieldType) is true (Dropdown/MultipleChoice/Checkboxes).
        /// </summary>
        public string? OptionsJson { get; set; }

        public virtual Form Form { get; set; } = null!;

        public virtual ICollection<FormAnswer> Answers { get; set; } = new List<FormAnswer>();
    }
}
