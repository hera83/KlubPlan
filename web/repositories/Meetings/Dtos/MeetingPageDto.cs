namespace web.Repositories.Meetings.Dtos;

/// <summary>One page of the meetings table. Page/PageSize are the values actually used (after clamping).</summary>
public class MeetingPageDto
{
    public List<MeetingListItemDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
