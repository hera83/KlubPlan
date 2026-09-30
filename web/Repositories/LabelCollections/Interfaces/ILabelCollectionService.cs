using web.Infrastructure.Labels;
using web.Repositories.LabelCollections.Dtos;
using web.ViewModels;

namespace web.Repositories.LabelCollections.Interfaces
{
    /// <summary>
    /// Værktøjer → Labels: named collections of labels designed in the label designer (text, løbenummer,
    /// images, QR codes), an image library per collection (App_files/labels/) and the A4 label-sheet PDF.
    /// </summary>
    public interface ILabelCollectionService
    {
        /// <summary>The table page matching the search, sorted by name.</summary>
        Task<LabelCollectionFilterViewModel> GetCollectionsAsync(LabelCollectionFilterViewModel filter, CancellationToken ct = default);

        /// <summary>Creates the collection with one starter label. Result.Id is the new collection's id.</summary>
        Task<LabelCollectionActionResultDto> CreateAsync(LabelCollectionCreateViewModel input, CancellationToken ct = default);

        /// <summary>Deletes the collection, its labels and its image files. False when not found.</summary>
        Task<bool> DeleteAsync(int id, CancellationToken ct = default);

        /// <summary>The design and image library for the designer (and the print preview). Null when not found.</summary>
        Task<LabelDesignerViewModel?> GetDesignerAsync(int id, CancellationToken ct = default);

        /// <summary>Validates/normalizes the design and replaces the collection's name, sheet grid and labels.</summary>
        Task<LabelCollectionActionResultDto> SaveDesignAsync(int id, LabelCollectionDesignDto design, CancellationToken ct = default);

        /// <summary>Adds an image to the collection's image library. Result.Media is the new file.</summary>
        Task<LabelCollectionActionResultDto> UploadMediaAsync(int id, Stream content, string fileName, long length, string? ownerId, CancellationToken ct = default);

        /// <summary>Removes an image from the library — refused while a saved label uses it.</summary>
        Task<LabelCollectionActionResultDto> DeleteMediaAsync(int id, Guid mediaId, CancellationToken ct = default);

        /// <summary>Physical path and content type of an image. Null when not found.</summary>
        Task<(string FullPath, string ContentType)?> GetMediaFileAsync(Guid mediaId, CancellationToken ct = default);

        /// <summary>The collection's labels, each repeated Quantity times, on its A4 sheet grid. Null when not found.</summary>
        Task<LabelPdfResult?> BuildPdfAsync(int id, bool cutMarks, CancellationToken ct = default);
    }
}
