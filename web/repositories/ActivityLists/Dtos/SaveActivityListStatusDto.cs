namespace web.Repositories.ActivityLists.Dtos;

public class SaveActivityListStatusDto
{
    /// <summary>Null/0 = new status.</summary>
    public int? Id { get; set; }
    public string? Name { get; set; }
    public string Color { get; set; } = "muted";
}
