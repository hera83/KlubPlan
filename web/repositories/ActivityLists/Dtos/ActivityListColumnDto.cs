using web.Data.Models;

namespace web.Repositories.ActivityLists.Dtos;

public class ActivityListColumnDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ActivityListColumnKind Kind { get; set; }

    /// <summary>The choices of a Choice column.</summary>
    public List<string> Options { get; set; } = [];
    public bool IsHidden { get; set; }
}
