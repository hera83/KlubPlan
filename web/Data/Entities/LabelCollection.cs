namespace web.Data.Entities
{
    /// <summary>
    /// A named set of labels under Værktøjer → Labels, printed to A4 label sheets with "Print labels"
    /// (the same layout as labels on work list lines).
    /// </summary>
    public class LabelCollection
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAtUtc { get; set; }

        public virtual ICollection<LabelCollectionItem> Items { get; set; } = new List<LabelCollectionItem>();
    }
}
