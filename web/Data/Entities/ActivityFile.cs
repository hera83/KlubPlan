namespace web.Data.Entities
{
    /// <summary>
    /// A logical file in an Activity's "Filer" tab. Uploading a file with the same name (case
    /// insensitive) into the same folder adds a new ActivityFileVersion instead of overwriting, so
    /// earlier versions can always be found again. The newest version is what is shown/downloaded.
    /// </summary>
    public class ActivityFile
    {
        public int Id { get; set; }

        public int ActivityId { get; set; }

        /// <summary>Null = root level of the activity's files.</summary>
        public int? FolderId { get; set; }

        /// <summary>Display name incl. extension, e.g. "Budget.xlsx" — unique per folder (enforced in ActivityFileService).</summary>
        public string FileName { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>Set whenever a new version is added, or the file is renamed/moved.</summary>
        public DateTime? UpdatedAtUtc { get; set; }

        public virtual Activity Activity { get; set; } = null!;

        public virtual ActivityFolder? Folder { get; set; }

        public virtual ICollection<ActivityFileVersion> Versions { get; set; } = new List<ActivityFileVersion>();
    }
}
