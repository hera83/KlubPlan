using web.Repositories.LabelCollections.Dtos;

namespace web.Repositories.LabelCollections.Interfaces;

/// <summary>
/// Værktøjer → Labels: named collections of labels designed in the label designer (text, løbenummer,
/// images, QR codes) and an image library per collection. The image files themselves are FileMetadata
/// rows owned by the Files repository; this repository only stores which file belongs to which collection.
/// </summary>
public interface ILabelCollectionRepository
{
    /// <summary>The table page matching the search, sorted by name.</summary>
    Task<LabelCollectionPageDto> SearchAsync(SearchLabelCollectionsDto search, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Creates the collection with one starter label. Invalid when the name is blank.</summary>
    Task<SaveLabelCollectionResultDto> CreateAsync(CreateLabelCollectionDto dto, CancellationToken cancellationToken = default);

    /// <summary>Deletes the collection with its labels and image rows. Returns the ids of the stored files to delete, or null when not found.</summary>
    Task<List<int>?> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>The design and image library for the designer (and the print preview). Null when not found.</summary>
    Task<LabelCollectionDesignerDto?> GetDesignerAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Validates/normalizes the design (LabelCollectionDesignNormalizer) and replaces the name, sheet grid and labels.</summary>
    Task<SaveLabelCollectionResultDto> SaveDesignAsync(int id, LabelCollectionDesignDto design, CancellationToken cancellationToken = default);

    /// <summary>Adds a stored file to the collection's image library. Null when the collection doesn't exist.</summary>
    Task<LabelMediaDto?> AddMediaAsync(int id, int fileId, CancellationToken cancellationToken = default);

    /// <summary>Removes an image from the library — refused while a saved label uses it.</summary>
    Task<RemoveLabelCollectionMediaResultDto> RemoveMediaAsync(int id, Guid mediaId, CancellationToken cancellationToken = default);

    /// <summary>The stored file behind an image id (LabelCollectionMedia.PublicId), or null.</summary>
    Task<int?> GetMediaFileIdAsync(Guid mediaId, CancellationToken cancellationToken = default);

    /// <summary>The saved design and the files behind its images, for the PDF. Null when not found.</summary>
    Task<LabelCollectionPrintDto?> GetPrintAsync(int id, CancellationToken cancellationToken = default);
}
