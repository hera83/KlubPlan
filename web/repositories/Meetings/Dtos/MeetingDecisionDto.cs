namespace web.Repositories.Meetings.Dtos;

public class MeetingDecisionDto
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? ResponsibleUserId { get; set; }
    public string? ResponsibleUserName { get; set; }
    public DateOnly? DueDate { get; set; }
    public bool IsCompleted { get; set; }
}
