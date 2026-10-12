namespace web.Repositories.Meetings.Dtos;

public class MeetingAttendeeDto
{
    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool HasAttended { get; set; }
}
