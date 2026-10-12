using web.Managers.InfoScreens.Dtos;
using web.Repositories.Files.Dtos;
using web.Repositories.InfoScreens.Dtos;

namespace web.Managers.InfoScreens.Interfaces;

/// <summary>Info screen operations that also touch the stored media files (InfoScreens + Files repositories).</summary>
public interface IInfoScreenManager
{
    /// <summary>Checks type and size, stores the file under app_files/infoscreens/{id}/ and adds it to the media library.</summary>
    Task<UploadInfoScreenMediaResultDto> UploadMediaAsync(int id, Stream content, string fileName, long length, string? ownerId, CancellationToken cancellationToken = default);

    /// <summary>Removes a file from the media library and deletes it — refused while a saved slide uses it.</summary>
    Task<RemoveInfoScreenMediaStatus> DeleteMediaAsync(int id, Guid mediaId, CancellationToken cancellationToken = default);

    /// <summary>Deletes the screen, its slides and its media files. False when not found.</summary>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>The file behind a media id (InfoScreenMedia.PublicId), or null.</summary>
    Task<StoredFileDto?> GetMediaFileAsync(Guid mediaId, CancellationToken cancellationToken = default);
}
