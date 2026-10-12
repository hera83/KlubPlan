using web.Repositories.ActivityFiles.Dtos;

namespace web.Repositories.ActivityFiles.Interfaces;

/// <summary>
/// The "Filer" tab on an activity: folders, files and file versions. Each version points at a
/// stored file owned by the Files repository. Every method is scoped by activityId so an id from
/// another activity is never touched.
/// </summary>
public interface IActivityFileRepository
{
    /// <summary>Content of a folder (null = root), or — when searchText is set — matches across all folders. Null when the activity doesn't exist.</summary>
    Task<ActivityFolderContentDto?> GetFolderAsync(int activityId, int? folderId, string? searchText, CancellationToken cancellationToken = default);

    Task<List<ActivityFolderTreeItemDto>> GetFolderTreeAsync(int activityId, CancellationToken cancellationToken = default);

    Task<ActivityFileVersionsDto?> GetVersionsAsync(int activityId, int fileId, CancellationToken cancellationToken = default);

    /// <summary>The version to download (the current one when versionId is null), or null.</summary>
    Task<ActivityFileVersionRefDto?> GetVersionRefAsync(int activityId, int fileId, int? versionId, CancellationToken cancellationToken = default);

    /// <summary>The zip layout for a selection (current version of each file). Null when the activity doesn't exist.</summary>
    Task<ActivityFileZipPlanDto?> GetZipPlanAsync(int activityId, IReadOnlyCollection<int> fileIds, IReadOnlyCollection<int> folderIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates an upload (name, blocked types, size) and finds where it goes: the target file for a new
    /// version, or the folder — creating the folders of a dropped folder's path — and any same-named file.
    /// </summary>
    Task<UploadTargetDto> ResolveUploadTargetAsync(int activityId, int? folderId, string? relativePath, int? targetFileId, string fileName, long length, string? userId, CancellationToken cancellationToken = default);

    /// <summary>Adds a stored file as the next version of a file — creating the file first when it doesn't exist in the folder yet.</summary>
    Task<ActivityFileVersionAddedDto> AddVersionAsync(int activityId, int? existingFileId, int? folderId, string name, int storedFileId, string? userId, CancellationToken cancellationToken = default);

    /// <summary>An older version to restore, or null when not found.</summary>
    Task<RestoreSourceDto?> GetRestoreSourceAsync(int activityId, int versionId, CancellationToken cancellationToken = default);

    Task<ActivityFileActionResultDto> CreateFolderAsync(int activityId, int? parentFolderId, string name, string? userId, CancellationToken cancellationToken = default);

    Task<ActivityFileActionResultDto> RenameFolderAsync(int activityId, int folderId, string name, CancellationToken cancellationToken = default);

    Task<ActivityFileActionResultDto> RenameFileAsync(int activityId, int fileId, string name, CancellationToken cancellationToken = default);

    Task<MoveActivityFilesResultDto> MoveAsync(int activityId, IReadOnlyCollection<int> fileIds, IReadOnlyCollection<int> folderIds, int? targetFolderId, CancellationToken cancellationToken = default);

    Task<DeleteActivityFilesSummaryDto> GetDeleteSummaryAsync(int activityId, IReadOnlyCollection<int> fileIds, IReadOnlyCollection<int> folderIds, CancellationToken cancellationToken = default);

    /// <summary>Deletes the selection incl. sub folders and all versions (in one transaction). Returns what was deleted and the stored files to delete.</summary>
    Task<DeleteActivityFilesResultDto> DeleteAsync(int activityId, IReadOnlyCollection<int> fileIds, IReadOnlyCollection<int> folderIds, CancellationToken cancellationToken = default);

    /// <summary>Deletes every folder and file of the activity. Returns the stored files to delete.</summary>
    Task<List<int>> DeleteAllForActivityAsync(int activityId, CancellationToken cancellationToken = default);
}
