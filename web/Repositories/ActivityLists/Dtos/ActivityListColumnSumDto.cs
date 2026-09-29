namespace web.Repositories.ActivityLists.Dtos
{
    /// <summary>
    /// "Optælling": the sum of one column over the lines matching the table's current search/filter
    /// (the whole list when there's no filter). Only cells holding nothing but the
    /// digits 0-9 (after trimming spaces) are added up — every other line, incl. empty cells, is skipped.
    /// </summary>
    public class ActivityListColumnSumDto
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }

        public int ColumnId { get; set; }
        public string ColumnName { get; set; } = string.Empty;

        public decimal Sum { get; set; }

        /// <summary>The sum with Danish thousands separators, e.g. "12.345".</summary>
        public string SumText { get; set; } = "0";

        /// <summary>Lines whose value was added to the sum.</summary>
        public int Counted { get; set; }

        /// <summary>Lines skipped because the cell was empty or not a whole number.</summary>
        public int Skipped { get; set; }

        /// <summary>True when a search/filter narrowed the lines; false = the whole list was counted.</summary>
        public bool Filtered { get; set; }

        public static ActivityListColumnSumDto Fail(string error) => new() { Success = false, ErrorMessage = error };
    }
}
