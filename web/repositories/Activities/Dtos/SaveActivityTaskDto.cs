namespace web.Repositories.Activities.Dtos;

public class SaveActivityTaskDto
{
    /// <summary>0 = new task.</summary>
    public int Id { get; set; }
    public int ActivityId { get; set; }
    public string? Title { get; set; }
    public DateTime? DeadlineAtUtc { get; set; }
    public int? AssignedToWorkgroupMemberId { get; set; }
    public string? Note { get; set; }
}
