using web.Infrastructure.Labels;
using web.Managers.LabelCollections.Dtos;
using web.Repositories.Files.Dtos;
using web.Repositories.LabelCollections.Dtos;

namespace web.Managers.LabelCollections.Interfaces;

/// <summary>Label collection operations that also touch the stored images (LabelCollections + Files repositories).</summary>
public interface ILabelCollectionManager
{
    /// <summary>Checks type and size, stores the image under app_files/labels/{id}/ and adds it to the library.</summary>
    Task<UploadLabelMediaResultDto> UploadMediaAsync(int id, Stream content, string fileName, long length, string? ownerId, CancellationToken cancellationToken = default);

    /// <summary>Removes an image from the library and deletes it — refused while a saved label uses it.</summary>
    Task<RemoveLabelCollectionMediaStatus> DeleteMediaAsync(int id, Guid mediaId, CancellationToken cancellationToken = default);

    /// <summary>Deletes the collection, its labels and its image files. False when not found.</summary>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>The file behind an image id (LabelCollectionMedia.PublicId), or null.</summary>
    Task<StoredFileDto?> GetMediaFileAsync(Guid mediaId, CancellationToken cancellationToken = default);

    /// <summary>The collection's labels, each repeated Quantity times, on its A4 sheet grid. Null when not found.</summary>
    Task<LabelPdfResult?> BuildPdfAsync(int id, bool cutMarks, CancellationToken cancellationToken = default);
}
