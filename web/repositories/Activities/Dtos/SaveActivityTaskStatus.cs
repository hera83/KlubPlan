namespace web.Repositories.Activities.Dtos;

public enum SaveActivityTaskStatus
{
    Saved,
    TitleRequired,
    NotFound,
    ActivityNotFound,

    /// <summary>The chosen member isn't in this activity's arbejdsgruppe.</summary>
    AssigneeNotFound
}
