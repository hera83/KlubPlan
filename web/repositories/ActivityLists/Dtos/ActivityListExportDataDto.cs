namespace web.Repositories.ActivityLists.Dtos;

/// <summary>All lines matching a search (no paging) in its sort order — for the Excel export.</summary>
public class ActivityListExportDataDto
{
    public string Title { get; set; } = string.Empty;
    public ActivityListSchemaDto Schema { get; set; } = new();
    public List<ActivityListItemRowDto> Rows { get; set; } = [];
}
