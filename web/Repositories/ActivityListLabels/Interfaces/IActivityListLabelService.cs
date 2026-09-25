using web.Repositories.ActivityListLabels.Dtos;
using web.Repositories.ActivityLists.Dtos;
using web.ViewModels;

namespace web.Repositories.ActivityListLabels.Interfaces
{
    /// <summary>
    /// Labels on the lines of a work list ("Labels" on a line) and "Print labels", which lays them out
    /// on A4 label sheets as a PDF. Every method that takes an item id also takes the listId and
    /// verifies they belong together.
    /// </summary>
    public interface IActivityListLabelService
    {
        /// <summary>Null when the line isn't found on the list.</summary>
        Task<ActivityListItemLabelsViewModel?> GetItemLabelsAsync(int listId, int itemId, CancellationToken ct = default);

        /// <summary>Replaces the line's labels. Rows without text are skipped, so saving none removes them all.</summary>
        Task<ActivityListActionResultDto> SaveItemLabelsAsync(ActivityListItemLabelsSaveViewModel input, CancellationToken ct = default);

        /// <summary>Labels on the lines matching the filter (the whole list when it's empty). Null when the list doesn't exist.</summary>
        Task<ActivityListLabelCountDto?> CountAsync(ActivityListItemFilterViewModel filter, string? userId, CancellationToken ct = default);

        /// <summary>
        /// The labels on the lines matching the filter, each repeated Quantity times, in list order on
        /// A4 sheets with no page margin — starting top left on a fresh sheet. Null when the list doesn't exist.
        /// </summary>
        Task<ActivityListLabelPdfDto?> BuildPdfAsync(ActivityListLabelPrintViewModel input, string? userId, CancellationToken ct = default);
    }
}
