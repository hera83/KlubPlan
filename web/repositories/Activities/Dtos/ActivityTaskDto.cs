namespace web.Repositories.Activities.Dtos;

public class ActivityTaskDto
{
    public int Id { get; set; }
    public int ActivityId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime? DeadlineAtUtc { get; set; }
    public int? AssignedToWorkgroupMemberId { get; set; }
    public bool IsCompleted { get; set; }
    public string? Note { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public int Order { get; set; }
}
