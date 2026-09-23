using web.Repositories.ActivityFiles.Dtos;
using web.ViewModels;

namespace web.Repositories.ActivityFiles.Interfaces
{
    /// <summary>
    /// The "Filer" tab on an activity: folders, files and file versions. Physical files live under
    /// App_files/activities/{activityId}/ — only metadata is stored in the database. Every method is
    /// scoped by activityId so an id from another activity is never touched.
    /// </summary>
    public interface IActivityFileService
    {
        /// <summary>Content of a folder (null = root), or — when searchText is set — matches across all folders. Null when the activity doesn't exist.</summary>
        Task<ActivityFilesViewModel?> GetFolderAsync(int activityId, int? folderId, string? searchText, CancellationToken ct = default);

        Task<int> CountFilesAsync(int activityId, CancellationToken ct = default);

        Task<List<ActivityFolderTreeItemViewModel>> GetFolderTreeAsync(int activityId, CancellationToken ct = default);

        Task<ActivityFileVersionsViewModel?> GetVersionsAsync(int activityId, int fileId, CancellationToken ct = default);

        /// <summary>Current version when versionId is null.</summary>
        Task<ActivityFileDownloadDto?> GetDownloadAsync(int activityId, int fileId, int? versionId, CancellationToken ct = default);

        Task<ActivityFileZipDto?> GetZipAsync(int activityId, IReadOnlyCollection<int> fileIds, IReadOnlyCollection<int> folderIds, CancellationToken ct = default);

        Task<UploadActivityFileResponseDto> UploadAsync(UploadActivityFileRequestDto request, CancellationToken ct = default);

        /// <summary>Adds a copy of an older version as the new current version — history is kept.</summary>
        Task<ActivityFileActionResultDto> RestoreVersionAsync(int activityId, int versionId, string? userId, CancellationToken ct = default);

        Task<ActivityFileActionResultDto> CreateFolderAsync(int activityId, int? parentFolderId, string name, string? userId, CancellationToken ct = default);

        Task<ActivityFileActionResultDto> RenameFolderAsync(int activityId, int folderId, string name, CancellationToken ct = default);

        Task<ActivityFileActionResultDto> RenameFileAsync(int activityId, int fileId, string name, CancellationToken ct = default);

        Task<MoveActivityFilesResponseDto> MoveAsync(int activityId, IReadOnlyCollection<int> fileIds, IReadOnlyCollection<int> folderIds, int? targetFolderId, CancellationToken ct = default);

        Task<DeleteActivityFilesSummaryDto> GetDeleteSummaryAsync(int activityId, IReadOnlyCollection<int> fileIds, IReadOnlyCollection<int> folderIds, CancellationToken ct = default);

        /// <summary>Permanently deletes the selection incl. sub folders, all versions and the physical files. No recycle bin.</summary>
        Task<DeleteActivityFilesSummaryDto> DeleteAsync(int activityId, IReadOnlyCollection<int> fileIds, IReadOnlyCollection<int> folderIds, CancellationToken ct = default);

        /// <summary>Removes every file/folder of the activity — called before the activity itself is deleted.</summary>
        Task DeleteAllForActivityAsync(int activityId, CancellationToken ct = default);
    }
}
