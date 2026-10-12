namespace web.Repositories.Meetings.Dtos;

public class CreateMeetingVersionResultDto
{
    public CreateMeetingVersionStatus Status { get; set; }

    /// <summary>The new version's id (when Created).</summary>
    public int MeetingId { get; set; }
}
