namespace web.Repositories.ActivityLists.Dtos;

/// <summary>Columns (imported first), statuses and members of a list — what the table is drawn from.</summary>
public class ActivityListSchemaDto
{
    public int ListId { get; set; }
    public bool ShowNote { get; set; }
    public List<ActivityListColumnDto> Columns { get; set; } = [];
    public List<ActivityListStatusDto> Statuses { get; set; } = [];
    public List<ActivityListMemberDto> Members { get; set; } = [];

    /// <summary>The column "Optælling" sums, or null.</summary>
    public int? SumColumnId { get; set; }
}
