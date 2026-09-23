namespace web.Data.Entities
{
    /// <summary>
    /// A work list on an activity ("Lister"-fanen), created by uploading an Excel sheet: the first
    /// row becomes the imported columns, every following row an ActivityListItem. Each item also has
    /// the fixed work fields Status and Tilknyttet, plus an optional Note (ShowNote) and any extra
    /// columns added afterwards (ActivityListColumn with a non-Imported kind).
    /// </summary>
    public class ActivityList
    {
        public int Id { get; set; }

        public int ActivityId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        /// <summary>Name of the uploaded Excel file — shown for reference only, the file itself is not kept.</summary>
        public string? SourceFileName { get; set; }

        /// <summary>The Note column can be removed per list; Status and Tilknyttet are always there.</summary>
        public bool ShowNote { get; set; } = true;

        public string? CreatedByUserId { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAtUtc { get; set; }

        public virtual Activity Activity { get; set; } = null!;

        public virtual ICollection<ActivityListColumn> Columns { get; set; } = new List<ActivityListColumn>();

        public virtual ICollection<ActivityListStatus> Statuses { get; set; } = new List<ActivityListStatus>();

        public virtual ICollection<ActivityListItem> Items { get; set; } = new List<ActivityListItem>();
    }
}
