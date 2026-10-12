using web.Repositories.Meetings.Dtos;
using web.Repositories.PersonGroups.Dtos;
using web.Repositories.Users.Dtos;

namespace web.Managers.Meetings.Dtos;

/// <summary>The meeting page: the meeting plus the option lists for its edit dialogs.</summary>
public class MeetingDetailsDto
{
    public MeetingDto Meeting { get; set; } = new();
    public List<PersonGroupDto> Groups { get; set; } = [];

    /// <summary>Active administrators/developers — who can attend meetings.</summary>
    public List<UserOptionDto> Admins { get; set; } = [];
}
