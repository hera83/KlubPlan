namespace web.Repositories.Activities.Dtos;

public class ActivityPageDto
{
    public List<ActivityListItemDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
