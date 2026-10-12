using web.Constants;

namespace web.Repositories.Meetings.Dtos;

public class MeetingVersionOptionDto
{
    public int Id { get; set; }
    public int VersionNumber { get; set; }
    public DateTime MeetingDateUtc { get; set; }
    public MeetingStatus Status { get; set; }
    public bool IsCurrent { get; set; }
}
