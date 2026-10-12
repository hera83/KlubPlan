using web.Repositories.Arrangements.Dtos;
using web.Repositories.People.Dtos;
using web.Repositories.PersonGroups.Dtos;

namespace web.Managers.Arrangements.Dtos;

/// <summary>What the arrangement builder needs: the arrangement (null when creating) and the option lists for the access tab.</summary>
public class ArrangementBuilderDto
{
    public ArrangementDto? Arrangement { get; set; }
    public List<PersonGroupDto> Groups { get; set; } = [];
    public List<PersonOptionDto> People { get; set; } = [];
}
