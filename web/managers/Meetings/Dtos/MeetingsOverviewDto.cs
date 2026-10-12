using web.Repositories.Meetings.Dtos;
using web.Repositories.PersonGroups.Dtos;
using web.Repositories.Users.Dtos;

namespace web.Managers.Meetings.Dtos;

/// <summary>The meetings table plus what its filters and the create dialog need.</summary>
public class MeetingsOverviewDto
{
    public MeetingPageDto Meetings { get; set; } = new();
    public List<PersonGroupDto> Groups { get; set; } = [];

    /// <summary>Active administrators/developers — who can attend meetings.</summary>
    public List<UserOptionDto> Admins { get; set; } = [];
}
