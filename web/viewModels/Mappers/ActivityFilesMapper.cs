using web.Extensions;
using web.Repositories.ActivityFiles.Dtos;

namespace web.ViewModels.Mappers;

/// <summary>Maps the activity "Filer" tab's Dtos to view models (icons and preview flags are presentation).</summary>
public static class ActivityFilesMapper
{
    public static ActivityFilesViewModel ToActivityFilesViewModel(this ActivityFolderContentDto content) => new()
    {
        ActivityId = content.ActivityId,
        CurrentFolderId = content.CurrentFolderId,
        SearchText = content.SearchText,
        TotalFileCount = content.TotalFileCount,
        TotalSizeBytes = content.TotalSizeBytes,
        Breadcrumbs = content.Breadcrumbs.Select(b => new ActivityFolderBreadcrumbViewModel { Id = b.Id, Name = b.Name }).ToList(),
        Folders = content.Folders.Select(f => new ActivityFolderItemViewModel
        {
            Id = f.Id,
            Name = f.Name,
            ItemCount = f.ItemCount,
            ModifiedAtUtc = f.ModifiedAtUtc,
            ParentPath = f.ParentPath
        }).ToList(),
        Files = content.Files.Select(f => new ActivityFileItemViewModel
        {
            Id = f.Id,
            FileName = f.FileName,
            IconClass = FileIconHelper.GetIconClass(f.FileName),
            FolderId = f.FolderId,
            FolderPath = f.FolderPath,
            CurrentVersionId = f.CurrentVersionId,
            CurrentVersionNumber = f.CurrentVersionNumber,
            VersionCount = f.VersionCount,
            SizeBytes = f.SizeBytes,
            ContentType = f.ContentType,
            CanPreview = FileIconHelper.CanPreview(f.ContentType),
            ModifiedAtUtc = f.ModifiedAtUtc,
            ModifiedByName = f.ModifiedByName
        }).ToList()
    };

    public static List<ActivityFolderTreeItemViewModel> ToActivityFolderTreeItemViewModels(this List<ActivityFolderTreeItemDto> tree) =>
        tree.Select(t => new ActivityFolderTreeItemViewModel { Id = t.Id, Name = t.Name, Depth = t.Depth }).ToList();

    public static ActivityFileVersionsViewModel ToActivityFileVersionsViewModel(this ActivityFileVersionsDto versions) => new()
    {
        ActivityId = versions.ActivityId,
        FileId = versions.FileId,
        FileName = versions.FileName,
        IconClass = FileIconHelper.GetIconClass(versions.FileName),
        CanPreview = versions.Versions.Count > 0 && FileIconHelper.CanPreview(versions.Versions[0].ContentType),
        Versions = versions.Versions.Select(v => new ActivityFileVersionItemViewModel
        {
            Id = v.Id,
            VersionNumber = v.VersionNumber,
            SizeBytes = v.SizeBytes,
            UploadedByName = v.UploadedByName,
            CreatedAtUtc = v.CreatedAtUtc,
            IsCurrent = v.IsCurrent
        }).ToList()
    };
}
