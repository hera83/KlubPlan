namespace web.Data.Entities
{
    /// <summary>The value of one column on one line. YesNo columns store "true" or nothing.</summary>
    public class ActivityListCellValue
    {
        public int Id { get; set; }

        public int ActivityListItemId { get; set; }

        public int ActivityListColumnId { get; set; }

        public string? Value { get; set; }

        public virtual ActivityListItem Item { get; set; } = null!;

        public virtual ActivityListColumn Column { get; set; } = null!;
    }
}
