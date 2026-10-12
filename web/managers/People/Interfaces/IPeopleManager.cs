using web.Managers.People.Dtos;
using web.Repositories.People.Dtos;

namespace web.Managers.People.Interfaces;

public interface IPeopleManager
{
    Task<PeopleOverviewDto> GetOverviewAsync(SearchPeopleDto search, CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports one person per line into the group as players. Lines are trimmed (also of surrounding
    /// commas), blank lines skipped, names title-cased (da-DK), and names over 200 characters rejected.
    /// </summary>
    Task<ImportPersonsResultDto> ImportToGroupAsync(int groupId, IReadOnlyList<string> rawNames, CancellationToken cancellationToken = default);
}
