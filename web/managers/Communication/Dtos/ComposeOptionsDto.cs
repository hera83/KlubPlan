using web.Repositories.Forms.Dtos;
using web.Repositories.People.Dtos;
using web.Repositories.PersonGroups.Dtos;

namespace web.Managers.Communication.Dtos;

/// <summary>The recipient and link pickers of the compose modal.</summary>
public class ComposeOptionsDto
{
    public List<PersonGroupDto> Groups { get; set; } = [];
    public List<PersonOptionDto> People { get; set; } = [];
    public List<FormLinkOptionDto> Forms { get; set; } = [];

    /// <summary>Not-yet-open and open arrangements — closed ones can't be linked.</summary>
    public List<ComposeArrangementOptionDto> Arrangements { get; set; } = [];
}
