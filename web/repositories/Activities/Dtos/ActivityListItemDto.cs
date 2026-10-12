namespace web.Repositories.Activities.Dtos;

public class ActivityListItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Location { get; set; }
    public DateTime? StartAtUtc { get; set; }
    public DateTime? EndAtUtc { get; set; }
    public bool IsCancelled { get; set; }
    public List<string> TargetGroupNames { get; set; } = [];
    public int TaskTotalCount { get; set; }
    public int TaskCompletedCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
