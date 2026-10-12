namespace web.Repositories.Meetings.Dtos;

/// <summary>An attachment that has just been marked as being transcribed.</summary>
public class TranscriptionJobDto
{
    public int AttachmentId { get; set; }
    public int MeetingId { get; set; }

    /// <summary>The audio file (Files repository).</summary>
    public int FileId { get; set; }
}
