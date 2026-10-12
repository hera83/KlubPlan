namespace web.Repositories.Activities.Dtos;

/// <summary>An activity with target groups, arbejdsgruppe, tasks and linked forms (unsorted — the caller orders for display).</summary>
public class ActivityDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Location { get; set; }
    public string? Category { get; set; }
    public DateTime? StartAtUtc { get; set; }
    public DateTime? EndAtUtc { get; set; }
    public bool IsCancelled { get; set; }
    public List<ActivityTargetGroupDto> TargetGroups { get; set; } = [];
    public List<ActivityWorkgroupMemberDto> WorkgroupMembers { get; set; } = [];
    public List<ActivityTaskDto> Tasks { get; set; } = [];
    public List<ActivityLinkedFormDto> LinkedForms { get; set; } = [];
}
