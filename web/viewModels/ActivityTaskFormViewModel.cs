namespace web.ViewModels;

/// <summary>The task modal on the activity page (_TaskModal).</summary>
public class ActivityTaskFormViewModel
{
    /// <summary>0 = new task.</summary>
    public int Id { get; set; }

    public int ActivityId { get; set; }

    public string? Title { get; set; }

    /// <summary>datetime-local value (browser-local time).</summary>
    public DateTime? DeadlineAt { get; set; }

    public int? AssignedToWorkgroupMemberId { get; set; }

    public string? Note { get; set; }
}
