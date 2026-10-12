namespace web.Repositories.ActivityLists.Dtos;

/// <summary>The public /Arbejdsliste page for one external contact. Found is false when the link isn't valid.</summary>
public class ActivityListPublicDto
{
    public bool Found { get; set; }
    public int ListId { get; set; }
    public Guid MemberPublicId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string ActivityTitle { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ActivityListSchemaDto Schema { get; set; } = new();
    public ActivityListItemPageDto Items { get; set; } = new();
}
