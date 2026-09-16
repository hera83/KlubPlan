namespace web.Data.Entities
{
    /// <summary>
    /// Links a Meeting to an uploaded file (bilag or a dictaphone recording). The physical file
    /// and its metadata live in FileMetadata as usual — this just associates it with a meeting.
    /// </summary>
    public class MeetingAttachment
    {
        public int Id { get; set; }

        public int MeetingId { get; set; }

        public int FileMetadataId { get; set; }

        /// <summary>True when this attachment is a dictaphone recording rather than a manually uploaded file.</summary>
        public bool IsRecording { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public virtual Meeting Meeting { get; set; } = null!;

        public virtual FileMetadata FileMetadata { get; set; } = null!;
    }
}
