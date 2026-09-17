using web.Constants;

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

        /// <summary>State of the background transcription job for this attachment, if any was started.</summary>
        public TranscriptionStatus TranscriptionStatus { get; set; } = TranscriptionStatus.None;

        public DateTime? TranscriptionStartedAtUtc { get; set; }

        public DateTime? TranscriptionCompletedAtUtc { get; set; }

        public string? TranscriptionError { get; set; }

        public virtual Meeting Meeting { get; set; } = null!;

        public virtual FileMetadata FileMetadata { get; set; } = null!;
    }
}
