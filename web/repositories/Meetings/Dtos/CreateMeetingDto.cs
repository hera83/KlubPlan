namespace web.Repositories.Meetings.Dtos;

public class CreateMeetingDto
{
    public string Title { get; set; } = string.Empty;
    public DateTime MeetingDateUtc { get; set; }
    public string? Location { get; set; }
    public List<int> GroupIds { get; set; } = [];
    public List<string> AttendeeUserIds { get; set; } = [];
    public string? AgendaNotes { get; set; }
}
