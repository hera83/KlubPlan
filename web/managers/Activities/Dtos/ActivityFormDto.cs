using web.Repositories.Activities.Dtos;
using web.Repositories.PersonGroups.Dtos;

namespace web.Managers.Activities.Dtos;

/// <summary>The create/edit form: the activity (null when creating) and the group picker.</summary>
public class ActivityFormDto
{
    public ActivityDto? Activity { get; set; }
    public List<PersonGroupDto> Groups { get; set; } = [];
}
