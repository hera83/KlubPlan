using web.Constants;

namespace web.Repositories.Meetings.Dtos;

public class SearchMeetingsDto
{
    /// <summary>Case-insensitive substring of title or location.</summary>
    public string? SearchText { get; set; }

    public MeetingStatus? Status { get; set; }

    /// <summary>Only meetings for at least one of these groups. Empty = all.</summary>
    public List<int> GroupIds { get; set; } = [];

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
