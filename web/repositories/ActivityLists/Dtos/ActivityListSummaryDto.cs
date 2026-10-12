namespace web.Repositories.ActivityLists.Dtos;

/// <summary>A list on the activity's "Lister" tab.</summary>
public class ActivityListSummaryDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? SourceFileName { get; set; }
    public int ItemCount { get; set; }

    /// <summary>Lines assigned to the current user's workgroup membership.</summary>
    public int MineCount { get; set; }
    public int UnassignedCount { get; set; }
    public List<ActivityListStatusDto> Statuses { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public string? CreatedByName { get; set; }
}
