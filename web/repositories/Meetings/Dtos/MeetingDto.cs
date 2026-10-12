using web.Constants;

namespace web.Repositories.Meetings.Dtos;

/// <summary>One meeting with groups, attendees, decisions, attachments and where it sits in its version series.</summary>
public class MeetingDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime MeetingDateUtc { get; set; }
    public string? Location { get; set; }
    public MeetingStatus Status { get; set; }
    public string? AgendaNotes { get; set; }
    public string? MinutesNotes { get; set; }

    /// <summary>By group name.</summary>
    public List<MeetingGroupDto> Groups { get; set; } = [];

    /// <summary>By display name.</summary>
    public List<MeetingAttendeeDto> Attendees { get; set; } = [];

    /// <summary>Oldest first.</summary>
    public List<MeetingDecisionDto> Decisions { get; set; } = [];

    /// <summary>Newest first.</summary>
    public List<MeetingAttachmentDto> Attachments { get; set; } = [];

    public MeetingVersionInfoDto VersionInfo { get; set; } = new();
}
