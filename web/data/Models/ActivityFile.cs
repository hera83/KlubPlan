namespace web.Data.Models;

/// <summary>
/// A logical file in an Activity's "Filer" tab. Uploading a file with the same name (case
/// insensitive) into the same folder adds a new ActivityFileVersion instead of overwriting, so
/// earlier versions can always be found again. The newest version is what is shown/downloaded.
/// </summary>
public class ActivityFile : EntityBase
{
    public int ActivityId { get; set; }

    /// <summary>Null = root level of the activity's files.</summary>
    public int? FolderId { get; set; }

    /// <summary>Display name incl. extension, e.g. "Budget.xlsx" — unique per folder (enforced in ActivityFileRepository).</summary>
    public string FileName { get; set; } = string.Empty;

    public virtual Activity Activity { get; set; } = null!;

    public virtual ActivityFolder? Folder { get; set; }

    public virtual ICollection<ActivityFileVersion> Versions { get; set; } = new List<ActivityFileVersion>();
}
