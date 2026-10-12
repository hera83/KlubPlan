using web.Data.Models;

namespace web.Repositories.ActivityLists.Dtos;

public class SaveActivityListColumnDto
{
    public int ListId { get; set; }

    /// <summary>Null = new column.</summary>
    public int? ColumnId { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Only used for new columns — the kind of an existing column can't change.</summary>
    public ActivityListColumnKind Kind { get; set; }

    /// <summary>One choice per line (Choice columns).</summary>
    public string? Options { get; set; }
}
