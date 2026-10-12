namespace web.Repositories.ActivityLists.Dtos;

public class ActivityListStatusDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "muted";
    public bool IsDefault { get; set; }

    /// <summary>Lines with this status (on the public link: only the member's own lines).</summary>
    public int Count { get; set; }
}
