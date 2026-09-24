namespace web.Data.Entities
{
    /// <summary>A column in an ActivityList — either imported from the Excel header row or an extra column added afterwards.</summary>
    public class ActivityListColumn
    {
        public int Id { get; set; }

        public int ActivityListId { get; set; }

        public string Name { get; set; } = string.Empty;

        public ActivityListColumnKind Kind { get; set; }

        /// <summary>Choice columns only: the allowed values, one per line.</summary>
        public string? Options { get; set; }

        public int Order { get; set; }

        /// <summary>Hidden from the list table to save space. Still searchable, editable in the line modal and included in exports.</summary>
        public bool IsHidden { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public virtual ActivityList ActivityList { get; set; } = null!;

        public virtual ICollection<ActivityListCellValue> Values { get; set; } = new List<ActivityListCellValue>();
    }
}
