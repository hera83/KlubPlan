namespace web.Data.Models;

/// <summary>
/// A concrete decision/action point from a meeting, optionally assigned to one of the
/// attending administrators with a due date. Kept separate from the free-text minutes
/// since this is what people actually revisit after the meeting.
/// </summary>
public class MeetingDecision : EntityBase
{
    public int MeetingId { get; set; }

    public string Description { get; set; } = string.Empty;

    public string? ResponsibleUserId { get; set; }

    public DateOnly? DueDate { get; set; }

    public bool IsCompleted { get; set; }

    public virtual Meeting Meeting { get; set; } = null!;

    public virtual ApplicationUser? ResponsibleUser { get; set; }
}
