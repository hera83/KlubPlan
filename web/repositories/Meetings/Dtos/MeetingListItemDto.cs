using web.Constants;

namespace web.Repositories.Meetings.Dtos;

/// <summary>The latest version of a meeting series, as the meetings table shows it.</summary>
public class MeetingListItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime MeetingDateUtc { get; set; }
    public string? Location { get; set; }
    public MeetingStatus Status { get; set; }
    public List<MeetingGroupDto> Groups { get; set; } = [];
    public int AttendeeCount { get; set; }
    public int AttendedCount { get; set; }
    public int VersionNumber { get; set; }
    public int VersionCount { get; set; }

    /// <summary>All versions in the series, newest first — only filled when there is more than one.</summary>
    public List<MeetingVersionOptionDto> Versions { get; set; } = [];
}
