namespace web.Data.Models;

/// <summary>
/// A folder in an Activity's "Filer" tab. ParentFolderId null = root level. Deleting a folder
/// deletes everything below it (sub folders, files and all their versions) — no recycle bin.
/// </summary>
public class ActivityFolder : EntityBase
{
    public int ActivityId { get; set; }

    public int? ParentFolderId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? CreatedByUserId { get; set; }

    public virtual Activity Activity { get; set; } = null!;

    public virtual ActivityFolder? ParentFolder { get; set; }

    public virtual ICollection<ActivityFolder> SubFolders { get; set; } = new List<ActivityFolder>();

    public virtual ICollection<ActivityFile> Files { get; set; } = new List<ActivityFile>();
}
