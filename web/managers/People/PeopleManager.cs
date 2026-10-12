using System.Globalization;
using web.Managers.People.Dtos;
using web.Managers.People.Interfaces;
using web.Repositories.People.Dtos;
using web.Repositories.People.Interfaces;
using web.Repositories.PersonGroups.Interfaces;

namespace web.Managers.People;

public class PeopleManager(IPersonRepository personRepository, IPersonGroupRepository personGroupRepository) : IPeopleManager
{
    private const int MaxNameLength = 200;
    private static readonly CultureInfo DanishCulture = CultureInfo.GetCultureInfo("da-DK");

    public async Task<PeopleOverviewDto> GetOverviewAsync(SearchPeopleDto search, CancellationToken cancellationToken = default) => new()
    {
        People = await personRepository.SearchAsync(search, cancellationToken),
        Groups = await personGroupRepository.GetAllAsync(cancellationToken)
    };

    public async Task<ImportPersonsResultDto> ImportToGroupAsync(int groupId, IReadOnlyList<string> rawNames, CancellationToken cancellationToken = default)
    {
        if (!await personGroupRepository.ExistsAsync(groupId, cancellationToken))
        {
            return new ImportPersonsResultDto { Status = ImportPersonsStatus.GroupNotFound };
        }

        var errors = new List<ImportPersonErrorDto>();
        var names = new List<string>();

        foreach (var raw in rawNames)
        {
            var name = (raw ?? string.Empty).Trim().Trim(',').Trim();
            if (name.Length == 0)
            {
                continue;
            }

            if (name.Length > MaxNameLength)
            {
                errors.Add(new ImportPersonErrorDto { Name = name, Reason = "Navnet er for langt (maks. 200 tegn)." });
                continue;
            }

            names.Add(ToTitleCaseName(name));
        }

        if (names.Count == 0)
        {
            // Nothing valid survived cleaning: with per-line errors that is still a (zero) import the
            // caller can show; with no lines at all the input was simply empty.
            return errors.Count > 0
                ? new ImportPersonsResultDto { Status = ImportPersonsStatus.Imported, Errors = errors }
                : new ImportPersonsResultDto { Status = ImportPersonsStatus.NoNames };
        }

        var imported = await personRepository.CreateManyInGroupAsync(groupId, names, cancellationToken);
        return imported is null
            ? new ImportPersonsResultDto { Status = ImportPersonsStatus.Failed }
            : new ImportPersonsResultDto { Status = ImportPersonsStatus.Imported, ImportedCount = imported.Value, Errors = errors };
    }

    private static string ToTitleCaseName(string name)
    {
        var words = name.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < words.Length; i++)
        {
            words[i] = DanishCulture.TextInfo.ToTitleCase(words[i].ToLower(DanishCulture));
        }

        return string.Join(' ', words);
    }
}
