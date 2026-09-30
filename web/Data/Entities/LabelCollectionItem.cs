using web.Infrastructure.Labels;

namespace web.Data.Entities
{
    /// <summary>One label in a LabelCollection. Quantity is the number of copies on the printed sheets.</summary>
    public class LabelCollectionItem : ILabelEntity
    {
        public int Id { get; set; }

        public int LabelCollectionId { get; set; }

        /// <summary>The label text. Line breaks are kept on the printed label.</summary>
        public string Text { get; set; } = string.Empty;

        public int Quantity { get; set; } = 1;

        /// <summary>Position in the collection — the print order.</summary>
        public int Order { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAtUtc { get; set; }

        public virtual LabelCollection Collection { get; set; } = null!;
    }
}
