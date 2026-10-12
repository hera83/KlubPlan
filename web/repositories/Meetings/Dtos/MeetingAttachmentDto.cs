using web.Constants;

namespace web.Repositories.Meetings.Dtos;

public class MeetingAttachmentDto
{
    public int Id { get; set; }

    /// <summary>The stored file (Files repository).</summary>
    public int FileId { get; set; }

    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }

    /// <summary>Recorded in the browser on the meeting page (rather than uploaded).</summary>
    public bool IsRecording { get; set; }

    public DateTime CreatedAt { get; set; }
    public TranscriptionStatus TranscriptionStatus { get; set; }
    public string? TranscriptionError { get; set; }
}
