using System.Globalization;
using Microsoft.AspNetCore.StaticFiles;
using web.Constants;
using web.Extensions;
using web.Managers.ActivityFiles.Dtos;
using web.Managers.ActivityFiles.Interfaces;
using web.Repositories.ActivityFiles.Dtos;
using web.Repositories.ActivityFiles.Interfaces;
using web.Repositories.Files.Dtos;
using web.Repositories.Files.Interfaces;

namespace web.Managers.ActivityFiles;

public class ActivityFileManager(
    IActivityFileRepository activityFileRepository,
    IFileRepository fileRepository,
    ILogger<ActivityFileManager> logger) : IActivityFileManager
{
    private static readonly FileExtensionContentTypeProvider ContentTypes = new();

    public async Task<UploadActivityFileResultDto> UploadAsync(UploadActivityFileDto upload, Stream content, CancellationToken cancellationToken = default)
    {
        var target = await activityFileRepository.ResolveUploadTargetAsync(
            upload.ActivityId, upload.FolderId, upload.RelativePath, upload.TargetFileId, upload.FileName, upload.Length, upload.UserId, cancellationToken);
        if (target.ErrorMessage is not null)
            return new UploadActivityFileResultDto { Success = false, ErrorMessage = target.ErrorMessage };

        var added = await StoreAsNewVersionAsync(upload.ActivityId, target.ExistingFileId, target.FolderId, target.Name, content, upload.UserId, cancellationToken);
        return new UploadActivityFileResultDto { Success = true, FileId = added.FileId, FileName = added.FileName, VersionNumber = added.VersionNumber };
    }

    public async Task<ActivityFileActionResultDto> RestoreVersionAsync(int activityId, int versionId, string? userId, CancellationToken cancellationToken = default)
    {
        var source = await activityFileRepository.GetRestoreSourceAsync(activityId, versionId, cancellationToken);
        if (source is null)
            return ActivityFileActionResultDto.Fail("Versionen blev ikke fundet.");

        var sourceFile = await fileRepository.GetByIdAsync(source.StoredFileId, cancellationToken);
        if (sourceFile is null)
            return ActivityFileActionResultDto.Fail("Filen til versionen findes ikke længere på disken.");
        if (source.IsLatest)
            return ActivityFileActionResultDto.Fail("Versionen er allerede den nyeste.");

        ActivityFileVersionAddedDto added;
        await using (var stream = File.OpenRead(sourceFile.FullPath))
        {
            added = await StoreAsNewVersionAsync(activityId, source.FileId, null, source.FileName, stream, userId, cancellationToken);
        }

        logger.LogInformation("Activity {ActivityId}: restored {FileName} version {VersionId} as version {New}", activityId, source.FileName, versionId, added.VersionNumber);
        return ActivityFileActionResultDto.Ok(added.VersionNumber);
    }

    public async Task<ActivityFileDownloadDto?> GetDownloadAsync(int activityId, int fileId, int? versionId, CancellationToken cancellationToken = default)
    {
        var version = await activityFileRepository.GetVersionRefAsync(activityId, fileId, versionId, cancellationToken);
        if (version is null)
            return null;

        var file = await fileRepository.GetByIdAsync(version.StoredFileId, cancellationToken);
        if (file is null)
        {
            logger.LogWarning("Activity file {FileId} version {Version} is missing on disk", fileId, version.VersionNumber);
            return null;
        }

        return new ActivityFileDownloadDto
        {
            FullPath = file.FullPath,
            ContentType = string.IsNullOrEmpty(version.ContentType) ? "application/octet-stream" : version.ContentType,
            FileName = version.DownloadName,
            CanPreview = FileIconHelper.CanPreview(version.ContentType)
        };
    }

    public async Task<ActivityFileZipDto?> GetZipAsync(int activityId, IReadOnlyCollection<int> fileIds, IReadOnlyCollection<int> folderIds, CancellationToken cancellationToken = default)
    {
        var plan = await activityFileRepository.GetZipPlanAsync(activityId, fileIds, folderIds, cancellationToken);
        if (plan is null)
            return null;

        var zip = new ActivityFileZipDto { ZipFileName = plan.ZipFileName, EmptyFolders = plan.EmptyFolders };
        foreach (var entry in plan.Entries)
        {
            // Files missing on disk are left out of the zip.
            var file = await fileRepository.GetByIdAsync(entry.StoredFileId, cancellationToken);
            if (file is not null)
                zip.Entries.Add(new ActivityFileZipEntryDto { EntryPath = entry.EntryPath, FullPath = file.FullPath });
        }

        return zip.Entries.Count == 0 && zip.EmptyFolders.Count == 0 ? null : zip;
    }

    public async Task<DeleteActivityFilesSummaryDto> DeleteAsync(int activityId, IReadOnlyCollection<int> fileIds, IReadOnlyCollection<int> folderIds, CancellationToken cancellationToken = default)
    {
        var result = await activityFileRepository.DeleteAsync(activityId, fileIds, folderIds, cancellationToken);
        foreach (var storedFileId in result.StoredFileIds)
            await fileRepository.DeleteAsync(storedFileId, cancellationToken);
        return result.Summary;
    }

    /// <summary>Stores the content and adds it as the next version; the stored file is deleted again if the version can't be saved.</summary>
    private async Task<ActivityFileVersionAddedDto> StoreAsNewVersionAsync(int activityId, int? existingFileId, int? folderId, string name, Stream content, string? userId, CancellationToken cancellationToken)
    {
        var stored = await fileRepository.CreateAsync(new CreateStoredFileDto
        {
            OriginalFileName = name,
            // The browser-supplied type isn't trusted — derive it from the extension.
            ContentType = ContentTypes.TryGetContentType(name, out var contentType) ? contentType : "application/octet-stream",
            Category = FileCategories.Activities,
            SubFolder = activityId.ToString(CultureInfo.InvariantCulture),
            OwnerId = userId
        }, content, cancellationToken);

        try
        {
            return await activityFileRepository.AddVersionAsync(activityId, existingFileId, folderId, name, stored.Id, userId, cancellationToken);
        }
        catch
        {
            await fileRepository.DeleteAsync(stored.Id, CancellationToken.None);
            throw;
        }
    }
}
