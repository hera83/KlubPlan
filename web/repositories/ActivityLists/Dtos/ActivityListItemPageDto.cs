namespace web.Repositories.ActivityLists.Dtos;

/// <summary>One page of lines — echoes the search (with the page adjusted to the last page when it's past the end).</summary>
public class ActivityListItemPageDto
{
    public int ListId { get; set; }
    public string? SearchText { get; set; }
    public int? StatusId { get; set; }
    public string? Assigned { get; set; }
    public string? NoteFilter { get; set; }
    public string? SortColumn { get; set; }
    public string? SortDir { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public ActivityListSchemaDto Schema { get; set; } = new();
    public List<ActivityListItemRowDto> Rows { get; set; } = [];

    /// <summary>Set on the public link's pages.</summary>
    public Guid? MemberPublicId { get; set; }
}
