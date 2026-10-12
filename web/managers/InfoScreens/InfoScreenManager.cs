using System.Globalization;
using web.Constants;
using web.Managers.InfoScreens.Dtos;
using web.Managers.InfoScreens.Interfaces;
using web.Repositories.Files.Dtos;
using web.Repositories.Files.Interfaces;
using web.Repositories.InfoScreens.Dtos;
using web.Repositories.InfoScreens.Interfaces;

namespace web.Managers.InfoScreens;

public class InfoScreenManager(
    IInfoScreenRepository infoScreenRepository,
    IFileRepository fileRepository,
    ILogger<InfoScreenManager> logger) : IInfoScreenManager
{
    public async Task<UploadInfoScreenMediaResultDto> UploadMediaAsync(int id, Stream content, string fileName, long length, string? ownerId, CancellationToken cancellationToken = default)
    {
        if (!await infoScreenRepository.ExistsAsync(id, cancellationToken))
            return new UploadInfoScreenMediaResultDto { Status = UploadInfoScreenMediaStatus.NotFound };

        // The content type comes from our own list, never from the browser.
        var extension = Path.GetExtension(fileName);
        string kind, contentType;
        long maxBytes;
        if (InfoScreenRules.ImageTypes.TryGetValue(extension, out var imageType))
            (kind, contentType, maxBytes) = (InfoScreenRules.MediaKindImage, imageType, InfoScreenRules.MaxImageBytes);
        else if (InfoScreenRules.VideoTypes.TryGetValue(extension, out var videoType))
            (kind, contentType, maxBytes) = (InfoScreenRules.MediaKindVideo, videoType, InfoScreenRules.MaxVideoBytes);
        else
            return Invalid("Filtypen understøttes ikke. Brug billeder (PNG, JPG, GIF, WEBP) eller video (MP4, WEBM).");

        if (length <= 0)
            return Invalid("Filen er tom.");
        if (length > maxBytes)
            return Invalid($"Filen er for stor. {(kind == InfoScreenRules.MediaKindImage ? "Billeder" : "Videoer")} må højst være {maxBytes / (1024 * 1024)} MB.");

        var file = await fileRepository.CreateAsync(new CreateStoredFileDto
        {
            OriginalFileName = Path.GetFileName(fileName),
            ContentType = contentType,
            Category = FileCategories.InfoScreens,
            SubFolder = id.ToString(CultureInfo.InvariantCulture),
            OwnerId = ownerId
        }, content, cancellationToken);

        var media = await infoScreenRepository.AddMediaAsync(id, file.Id, kind, cancellationToken);
        if (media is null)
        {
            // The screen was deleted while the file uploaded.
            await fileRepository.DeleteAsync(file.Id, cancellationToken);
            return new UploadInfoScreenMediaResultDto { Status = UploadInfoScreenMediaStatus.NotFound };
        }

        logger.LogInformation("Info screen {ScreenId} got {Kind} {FileId}", id, kind, file.Id);
        return new UploadInfoScreenMediaResultDto { Status = UploadInfoScreenMediaStatus.Uploaded, Media = media };
    }

    public async Task<RemoveInfoScreenMediaStatus> DeleteMediaAsync(int id, Guid mediaId, CancellationToken cancellationToken = default)
    {
        var result = await infoScreenRepository.RemoveMediaAsync(id, mediaId, cancellationToken);
        if (result.Status == RemoveInfoScreenMediaStatus.Removed)
            await fileRepository.DeleteAsync(result.FileId, cancellationToken);
        return result.Status;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var fileIds = await infoScreenRepository.DeleteAsync(id, cancellationToken);
        if (fileIds is null)
            return false;

        foreach (var fileId in fileIds)
            await fileRepository.DeleteAsync(fileId, cancellationToken);

        logger.LogInformation("Info screen {ScreenId} deleted with {FileCount} media files", id, fileIds.Count);
        return true;
    }

    public async Task<StoredFileDto?> GetMediaFileAsync(Guid mediaId, CancellationToken cancellationToken = default)
    {
        var fileId = await infoScreenRepository.GetMediaFileIdAsync(mediaId, cancellationToken);
        return fileId is null ? null : await fileRepository.GetByIdAsync(fileId.Value, cancellationToken);
    }

    private static UploadInfoScreenMediaResultDto Invalid(string error) =>
        new() { Status = UploadInfoScreenMediaStatus.Invalid, ErrorMessage = error };
}
