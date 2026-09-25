namespace web.Data.Entities
{
    /// <summary>
    /// A label to print for one line in an ActivityList ("Labels" on the line). Quantity is the number
    /// of copies that end up on the label sheets made by "Print labels".
    /// </summary>
    public class ActivityListLabel
    {
        public int Id { get; set; }

        public int ActivityListItemId { get; set; }

        /// <summary>The label text. Line breaks are kept on the printed label.</summary>
        public string Text { get; set; } = string.Empty;

        public int Quantity { get; set; } = 1;

        /// <summary>Position among the line's labels — the print order within the line.</summary>
        public int Order { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAtUtc { get; set; }

        public virtual ActivityListItem Item { get; set; } = null!;
    }
}
