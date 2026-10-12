using web.Constants;

namespace web.Repositories.Meetings.Dtos;

public class TranscriptionStatusDto
{
    public int AttachmentId { get; set; }
    public TranscriptionStatus Status { get; set; }
    public string? Error { get; set; }
}
