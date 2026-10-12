namespace web.Repositories.ActivityLists.Dtos;

public class ActivityListItemLabelsDto
{
    public int ItemId { get; set; }
    public int RowNumber { get; set; }
    public List<ActivityListLabelDto> Labels { get; set; } = [];
}
