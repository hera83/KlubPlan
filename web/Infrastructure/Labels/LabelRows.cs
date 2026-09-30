using web.Constants;
using web.ViewModels;

namespace web.Infrastructure.Labels
{
    /// <summary>
    /// Saving the rows from the shared label editor (Views/Shared/Partials/_LabelRowsEditor.cshtml):
    /// normalize → validate → apply onto the stored labels. Used by every place labels are edited.
    /// </summary>
    public static class LabelRows
    {
        /// <summary>Trimmed texts with \n line breaks; rows without text are skipped.</summary>
        public static List<LabelRow> Normalize(IEnumerable<LabelInputViewModel> inputs)
            => inputs
                .Select(l => new LabelRow(NormalizeText(l.Text), l.Quantity))
                .Where(l => l.Text.Length > 0)
                .ToList();

        /// <summary>Null when the rows are valid, else a Danish error for the toast.</summary>
        public static string? Validate(IReadOnlyCollection<LabelRow> rows, int maxRows, string tooManyMessage)
        {
            if (rows.Count > maxRows)
                return tooManyMessage;
            if (rows.Any(r => r.Text.Length > LabelRules.MaxTextLength))
                return $"Teksten på en label må højst være {LabelRules.MaxTextLength} tegn.";
            if (rows.Any(r => r.Quantity is < 1 or > LabelRules.MaxQuantity))
                return $"Antal skal være mellem 1 og {LabelRules.MaxQuantity}.";
            return null;
        }

        /// <summary>
        /// Makes the stored labels match the rows: reuses the existing ones in order (keeps CreatedAtUtc),
        /// adds the extra rows via <paramref name="add"/> (row, order) and removes the leftovers via <paramref name="remove"/>.
        /// </summary>
        public static void Apply<T>(IEnumerable<T> existing, IReadOnlyList<LabelRow> rows, Action<LabelRow, int> add, Action<IEnumerable<T>> remove)
            where T : ILabelEntity
        {
            var ordered = existing.OrderBy(l => l.Order).ToList();
            var now = DateTime.UtcNow;
            for (var n = 0; n < rows.Count; n++)
            {
                var row = rows[n];
                if (n >= ordered.Count)
                {
                    add(row, n);
                    continue;
                }

                var label = ordered[n];
                if (label.Text == row.Text && label.Quantity == row.Quantity && label.Order == n)
                    continue;
                label.Text = row.Text;
                label.Quantity = row.Quantity;
                label.Order = n;
                label.UpdatedAtUtc = now;
            }
            remove(ordered.Skip(rows.Count).ToList());
        }

        private static string NormalizeText(string? text)
            => (text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Trim();
    }
}
