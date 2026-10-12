using web.Repositories.Activities.Dtos;

namespace web.Managers.Activities.Dtos;

public class ActivityListDto
{
    public ActivityPageDto Page { get; set; } = new();

    /// <summary>Number of groups — an activity for all of them shows "Alle".</summary>
    public int TotalGroupCount { get; set; }
}
