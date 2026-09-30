using web.Infrastructure.Labels;
using web.Repositories.LabelCollections.Dtos;
using web.ViewModels;

namespace web.Repositories.LabelCollections.Interfaces
{
    /// <summary>
    /// Værktøjer → Labels: named collections of labels (text + copies), printed to A4 label sheets
    /// with the shared "Print labels" (LabelSheetPdf) — the same as labels on work list lines.
    /// </summary>
    public interface ILabelCollectionService
    {
        /// <summary>The table page matching the search, sorted by name.</summary>
        Task<LabelCollectionFilterViewModel> GetCollectionsAsync(LabelCollectionFilterViewModel filter, CancellationToken ct = default);

        /// <summary>Name and labels for the edit modal. Null when not found.</summary>
        Task<LabelCollectionEditViewModel?> GetForEditAsync(int id, CancellationToken ct = default);

        /// <summary>Creates (no Id) or updates the collection and replaces its labels. Rows without text are skipped.</summary>
        Task<SaveLabelCollectionResponseDto> SaveAsync(LabelCollectionEditViewModel input, CancellationToken ct = default);

        /// <summary>False when not found.</summary>
        Task<bool> DeleteAsync(int id, CancellationToken ct = default);

        /// <summary>The collection's labels, each repeated Quantity times, on A4 sheets. Null when not found.</summary>
        Task<LabelPdfResult?> BuildPdfAsync(int id, LabelSheetViewModel sheet, CancellationToken ct = default);
    }
}
