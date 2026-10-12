namespace web.Repositories.ActivityLists.Dtos;

/// <summary>An inline edit of one field on a line.</summary>
public class UpdateActivityListFieldDto
{
    public int ListId { get; set; }
    public int ItemId { get; set; }

    /// <summary>ActivityListRules.FieldStatus/FieldNote/FieldAssigned/FieldColumn.</summary>
    public string Field { get; set; } = string.Empty;
    public int? ColumnId { get; set; }
    public string? Value { get; set; }
}
