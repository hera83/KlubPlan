namespace web.Infrastructure.Labels
{
    /// <summary>A stored label (ActivityListLabel, LabelCollectionItem) — lets <see cref="LabelRows.Apply"/> save any of them the same way.</summary>
    public interface ILabelEntity
    {
        string Text { get; set; }
        int Quantity { get; set; }

        /// <summary>Position among its siblings — the print order.</summary>
        int Order { get; set; }

        DateTime? UpdatedAtUtc { get; set; }
    }
}
