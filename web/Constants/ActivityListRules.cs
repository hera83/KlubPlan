namespace web.Constants
{
    /// <summary>Limits and defaults for the activity "Lister" tab (work lists imported from Excel).</summary>
    public static class ActivityListRules
    {
        /// <summary>Max size of the uploaded Excel file (10 MB).</summary>
        public const long MaxImportBytes = 10L * 1024 * 1024;

        public const int MaxImportRows = 10000;

        public const int MaxImportColumns = 100;

        public const int MaxCellLength = 4000;

        public const int MaxNoteLength = 2000;

        /// <summary>Labels on a line ("Labels"-knappen) and "Print labels".</summary>
        public const int MaxLabelTextLength = 500;
        public const int MaxLabelsPerItem = 50;
        public const int MaxLabelQuantity = 500;

        /// <summary>Grid on an A4 sheet: labels across (per row) and down (rows).</summary>
        public const int MaxLabelsAcross = 10;
        public const int MaxLabelsDown = 30;

        /// <summary>Upper limit for one PDF so a mistyped quantity can't generate an endless document.</summary>
        public const int MaxLabelsPerPdf = 10000;

        /// <summary>Field names used by the inline editing endpoint.</summary>
        public const string FieldStatus = "status";
        public const string FieldNote = "note";
        public const string FieldAssigned = "assigned";
        public const string FieldColumn = "column";

        /// <summary>Special values for the "Tilknyttet" filter (besides a workgroup member id).</summary>
        public const string AssignedMine = "mine";
        public const string AssignedNone = "none";

        /// <summary>Color keys a status can use — each maps to --clr-{key} / .badge-{key}. Label is the Danish UI name.</summary>
        public static readonly IReadOnlyList<(string Key, string Label)> StatusColors = new[]
        {
            ("muted", "Grå"),
            ("info", "Blå"),
            ("ok", "Grøn"),
            ("warn", "Gul"),
            ("alarm", "Rød")
        };

        /// <summary>Statuses every new list starts with. The first one is the default for new lines.</summary>
        public static readonly IReadOnlyList<(string Name, string Color)> DefaultStatuses = new[]
        {
            ("Ikke startet", "muted"),
            ("Ok", "ok"),
            ("Ikke ok", "alarm"),
            ("Kræver opfølgning", "warn")
        };

        public static bool IsValidColor(string? color) => StatusColors.Any(c => c.Key == color);
    }
}
