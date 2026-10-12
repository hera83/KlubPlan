using web.Repositories.Activities.Dtos;
using web.Repositories.Forms.Dtos;
using web.Repositories.Users.Dtos;

namespace web.Managers.Activities.Dtos;

public class ActivityDetailsDto
{
    public ActivityDto Activity { get; set; } = new();
    public int TotalGroupCount { get; set; }

    /// <summary>Number of persons in the target groups.</summary>
    public int TargetAudienceCount { get; set; }

    /// <summary>Active administrators/developers an arbejdsgruppe member can be linked to.</summary>
    public List<UserOptionDto> AdminOptions { get; set; } = [];

    /// <summary>Latest version of every form series that isn't linked yet.</summary>
    public List<FormLinkOptionDto> FormOptions { get; set; } = [];
}
