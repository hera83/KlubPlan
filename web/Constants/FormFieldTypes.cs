namespace web.Constants
{
    /// <summary>
    /// The kinds of fields a form can contain. Stored on FormField.FieldType as a string
    /// (see ApplicationDbContext) so values stay readable in the database and stable across
    /// enum reordering.
    /// </summary>
    public enum FormFieldType
    {
        ShortText,
        LongText,
        Number,
        Email,
        Phone,
        Date,
        Dropdown,
        MultipleChoice,
        Checkboxes,
        SectionHeading
    }

    /// <summary>
    /// Danish UI labels and Bootstrap Icons for each field type, plus small helpers used by
    /// both the builder and the fill-out/response rendering.
    /// </summary>
    public static class FormFieldTypes
    {
        private static readonly Dictionary<FormFieldType, string> _uiLabels = new()
        {
            { FormFieldType.ShortText, "Kort svar" },
            { FormFieldType.LongText, "Langt svar" },
            { FormFieldType.Number, "Tal" },
            { FormFieldType.Email, "Email" },
            { FormFieldType.Phone, "Telefonnummer" },
            { FormFieldType.Date, "Dato" },
            { FormFieldType.Dropdown, "Dropdown" },
            { FormFieldType.MultipleChoice, "Multiple choice" },
            { FormFieldType.Checkboxes, "Afkrydsningsfelter" },
            { FormFieldType.SectionHeading, "Informationstekst" }
        };

        private static readonly Dictionary<FormFieldType, string> _icons = new()
        {
            { FormFieldType.ShortText, "bi-input-cursor-text" },
            { FormFieldType.LongText, "bi-text-paragraph" },
            { FormFieldType.Number, "bi-123" },
            { FormFieldType.Email, "bi-envelope" },
            { FormFieldType.Phone, "bi-telephone" },
            { FormFieldType.Date, "bi-calendar3" },
            { FormFieldType.Dropdown, "bi-menu-button-wide" },
            { FormFieldType.MultipleChoice, "bi-ui-radios" },
            { FormFieldType.Checkboxes, "bi-ui-checks" },
            { FormFieldType.SectionHeading, "bi-card-text" }
        };

        /// <summary>Field types that store a fixed list of choices in FormField.OptionsJson.</summary>
        private static readonly HashSet<FormFieldType> _optionTypes = new()
        {
            FormFieldType.Dropdown,
            FormFieldType.MultipleChoice,
            FormFieldType.Checkboxes
        };

        public static string GetUILabel(FormFieldType type)
            => _uiLabels.TryGetValue(type, out var label) ? label : type.ToString();

        public static string GetIcon(FormFieldType type)
            => _icons.TryGetValue(type, out var icon) ? icon : "bi-input-cursor-text";

        public static bool HasOptions(FormFieldType type) => _optionTypes.Contains(type);

        /// <summary>SectionHeading is a visual break only — it never collects an answer.</summary>
        public static bool IsAnswerable(FormFieldType type) => type != FormFieldType.SectionHeading;

        public static bool AllowsMultipleValues(FormFieldType type) => type == FormFieldType.Checkboxes;

        public static IReadOnlyList<FormFieldType> AllTypes { get; } = Enum.GetValues<FormFieldType>();
    }
}
