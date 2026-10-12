namespace web.Repositories.ActivityFiles.Dtos;

/// <summary>A folder of an activity's "Filer" tab (or search results across all folders) with breadcrumbs and totals.</summary>
public class ActivityFolderContentDto
{
    public int ActivityId { get; set; }

    /// <summary>Null = the root.</summary>
    public int? CurrentFolderId { get; set; }

    /// <summary>Trimmed; null when not searching.</summary>
    public string? SearchText { get; set; }

    /// <summary>From "Filer" (the root) down to the current folder.</summary>
    public List<ActivityFolderBreadcrumbDto> Breadcrumbs { get; set; } = [];

    public List<ActivityFolderItemDto> Folders { get; set; } = [];
    public List<ActivityFileItemDto> Files { get; set; } = [];

    /// <summary>All files of the activity.</summary>
    public int TotalFileCount { get; set; }

    /// <summary>All versions of all files of the activity.</summary>
    public long TotalSizeBytes { get; set; }
}
