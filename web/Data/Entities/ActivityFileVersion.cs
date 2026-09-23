namespace web.Data.Entities
{
    /// <summary>
    /// One uploaded version of an ActivityFile. The physical file and its metadata live in
    /// FileMetadata (category "activities") as usual — this row just ties it to the logical file.
    /// </summary>
    public class ActivityFileVersion
    {
        public int Id { get; set; }

        public int ActivityFileId { get; set; }

        public int FileMetadataId { get; set; }

        /// <summary>1, 2, 3 … — the highest number is the current version.</summary>
        public int VersionNumber { get; set; }

        /// <summary>Kept even if the user is later deleted (FK is SetNull).</summary>
        public string? UploadedByUserId { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public virtual ActivityFile ActivityFile { get; set; } = null!;

        public virtual FileMetadata FileMetadata { get; set; } = null!;

        public virtual ApplicationUser? UploadedByUser { get; set; }
    }
}
