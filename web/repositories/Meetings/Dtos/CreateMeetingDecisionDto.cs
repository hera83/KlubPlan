namespace web.Repositories.Meetings.Dtos;

public class CreateMeetingDecisionDto
{
    public string Description { get; set; } = string.Empty;

    /// <summary>Blank = no responsible user.</summary>
    public string? ResponsibleUserId { get; set; }

    public DateOnly? DueDate { get; set; }
}
