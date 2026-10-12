using web.Constants;

namespace web.Repositories.Meetings.Dtos;

/// <summary>Replaces the meeting's fields, groups and attendees (attendance is kept for attendees who stay).</summary>
public class UpdateMeetingDto
{
    public string Title { get; set; } = string.Empty;
    public DateTime MeetingDateUtc { get; set; }
    public string? Location { get; set; }
    public List<int> GroupIds { get; set; } = [];
    public List<string> AttendeeUserIds { get; set; } = [];
    public string? AgendaNotes { get; set; }
    public MeetingStatus Status { get; set; }
}
