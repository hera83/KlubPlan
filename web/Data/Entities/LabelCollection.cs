namespace web.Data.Entities
{
    /// <summary>
    /// A named set of designed labels under Værktøjer → Labels, made in the label designer and printed to
    /// A4 label sheets. The sheet grid (labels across × down, portrait/landscape) is part of the collection,
    /// so the designer knows the labels' exact size in mm.
    /// </summary>
    public class LabelCollection
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        /// <summary>Labels across one A4 sheet (LabelRules.MaxAcross).</summary>
        public int Across { get; set; } = 3;

        /// <summary>Labels down one A4 sheet (LabelRules.MaxDown).</summary>
        public int Down { get; set; } = 8;

        public bool Landscape { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAtUtc { get; set; }

        public virtual ICollection<LabelCollectionItem> Items { get; set; } = new List<LabelCollectionItem>();

        public virtual ICollection<LabelCollectionMedia> Media { get; set; } = new List<LabelCollectionMedia>();
    }
}
