namespace web.Repositories.ActivityLists.Dtos;

/// <summary>The list page: the list, its schema and the first page of lines.</summary>
public class ActivityListDto
{
    public int Id { get; set; }
    public int ActivityId { get; set; }
    public string ActivityTitle { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? SourceFileName { get; set; }
    public DateTime CreatedAt { get; set; }
    public int TotalCount { get; set; }
    public int UnassignedCount { get; set; }
    public bool CurrentUserIsMember { get; set; }
    public ActivityListSchemaDto Schema { get; set; } = new();
    public ActivityListItemPageDto Items { get; set; } = new();
}
