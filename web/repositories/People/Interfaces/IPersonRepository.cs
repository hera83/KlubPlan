using web.Repositories.People.Dtos;

namespace web.Repositories.People.Interfaces;

public interface IPersonRepository
{
    Task<PersonPageDto> SearchAsync(SearchPeopleDto search, CancellationToken cancellationToken = default);

    Task<PersonDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>All people (id, UID, name) sorted by name, for picker lists.</summary>
    Task<List<PersonOptionDto>> GetOptionsAsync(CancellationToken cancellationToken = default);

    /// <summary>Number of persons in at least one of the groups (0 when none are given).</summary>
    Task<int> CountInGroupsAsync(IReadOnlyCollection<int> groupIds, CancellationToken cancellationToken = default);

    /// <summary>Contact info for everyone in the selected groups plus the selected persons (each person once), ordered by Id.</summary>
    Task<List<PersonContactDto>> GetContactsAsync(IReadOnlyCollection<int> groupIds, IReadOnlyCollection<int> personIds, CancellationToken cancellationToken = default);

    /// <summary>Is the person a member of at least one of the groups?</summary>
    Task<bool> IsInAnyGroupAsync(int personId, IReadOnlyCollection<int> groupIds, CancellationToken cancellationToken = default);

    /// <summary>The person behind a public link's UId (Person.PublicId), or null.</summary>
    Task<int?> GetIdByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default);

    Task<SavePersonResultDto> CreateAsync(CreatePersonDto dto, CancellationToken cancellationToken = default);

    Task<SavePersonResultDto> UpdateAsync(int id, UpdatePersonDto dto, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Swaps Person.PublicId for a fresh Guid, invalidating any public /Formular link that used the
    /// old value. Nothing else references PublicId (internal relations use the int Id), so this is
    /// always safe — e.g. if a link leaked.
    /// </summary>
    Task<bool> RegeneratePublicIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates one person per name as a Player in the group, with consecutive UIDs. Returns the number
    /// created, or null if the save failed. Names must already be cleaned; the group must exist.
    /// </summary>
    Task<int?> CreateManyInGroupAsync(int groupId, IReadOnlyList<string> names, CancellationToken cancellationToken = default);
}
