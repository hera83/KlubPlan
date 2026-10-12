using web.Managers.ActivityFiles.Dtos;
using web.Repositories.ActivityFiles.Dtos;

namespace web.Managers.ActivityFiles.Interfaces;

/// <summary>The activity "Filer" tab's operations that touch the stored files (ActivityFiles + Files repositories).</summary>
public interface IActivityFileManager
{
    /// <summary>Validates and stores the upload as a new file — or a new version of an existing one — under app_files/activities/{activityId}/.</summary>
    Task<UploadActivityFileResultDto> UploadAsync(UploadActivityFileDto upload, Stream content, CancellationToken cancellationToken = default);

    /// <summary>Adds a copy of an older version as the new current version — history is kept.</summary>
    Task<ActivityFileActionResultDto> RestoreVersionAsync(int activityId, int versionId, string? userId, CancellationToken cancellationToken = default);

    /// <summary>The file to download (the current version when versionId is null). Null when not found or missing on disk.</summary>
    Task<ActivityFileDownloadDto?> GetDownloadAsync(int activityId, int fileId, int? versionId, CancellationToken cancellationToken = default);

    /// <summary>The files and folders to put in a zip. Null when the activity doesn't exist or there is nothing to zip.</summary>
    Task<ActivityFileZipDto?> GetZipAsync(int activityId, IReadOnlyCollection<int> fileIds, IReadOnlyCollection<int> folderIds, CancellationToken cancellationToken = default);

    /// <summary>Permanently deletes the selection incl. sub folders, all versions and the stored files. No recycle bin.</summary>
    Task<DeleteActivityFilesSummaryDto> DeleteAsync(int activityId, IReadOnlyCollection<int> fileIds, IReadOnlyCollection<int> folderIds, CancellationToken cancellationToken = default);
}
