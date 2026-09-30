namespace web.Constants
{
    /// <summary>
    /// Limits for labels and "Print labels" — shared by Labels (Værktøjer) and labels on work list
    /// lines, so both validate and print the same way.
    /// </summary>
    public static class LabelRules
    {
        public const int MaxTextLength = 500;
        public const int MaxQuantity = 500;

        /// <summary>Grid on an A4 sheet: labels across (per row) and down (rows).</summary>
        public const int MaxAcross = 10;
        public const int MaxDown = 30;

        /// <summary>Upper limit for one PDF so a mistyped quantity can't generate an endless document.</summary>
        public const int MaxLabelsPerPdf = 10000;

        /// <summary>Labels (Værktøjer): a collection's name and how many different label designs it can hold.</summary>
        public const int MaxCollectionNameLength = 200;
        public const int MaxLabelsPerCollection = 100;
    }
}
