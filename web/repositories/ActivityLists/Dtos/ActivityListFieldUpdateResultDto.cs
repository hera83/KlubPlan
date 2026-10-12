namespace web.Repositories.ActivityLists.Dtos;

/// <summary>Result of an inline edit — includes fresh status counts so the page's progress bar can update without a reload.</summary>
public class ActivityListFieldUpdateResultDto
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<int, int> StatusCounts { get; set; } = new();
    public int UnassignedCount { get; set; }

    /// <summary>Who changed the line (account or external contact name), for the line's history hint.</summary>
    public string? UpdatedByName { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }
}
