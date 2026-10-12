namespace web.Repositories.ActivityLists.Dtos;

public class ActivityListItemRowDto
{
    public int Id { get; set; }
    public int RowNumber { get; set; }

    /// <summary>Cell values by column id.</summary>
    public Dictionary<int, string?> Values { get; set; } = [];
    public int? StatusId { get; set; }
    public string? Note { get; set; }
    public int? AssignedMemberId { get; set; }

    /// <summary>Null when the line hasn't been changed since it was created/imported.</summary>
    public DateTime? UpdatedAtUtc { get; set; }
    public string? UpdatedByName { get; set; }
    public int LabelCopies { get; set; }
}
