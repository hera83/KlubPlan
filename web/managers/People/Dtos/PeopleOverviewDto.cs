using web.Repositories.People.Dtos;
using web.Repositories.PersonGroups.Dtos;

namespace web.Managers.People.Dtos;

/// <summary>A page of people plus all groups (for the group filter and group management).</summary>
public class PeopleOverviewDto
{
    public PersonPageDto People { get; set; } = new();
    public List<PersonGroupDto> Groups { get; set; } = [];
}
