using web.Repositories.PersonGroups.Dtos;

namespace web.Repositories.PersonGroups.Interfaces;

public interface IPersonGroupRepository
{
    /// <summary>All groups ordered by name, with member counts.</summary>
    Task<List<PersonGroupDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Number of groups — used to show "Alle" when a selection covers every group.</summary>
    Task<int> CountAsync(CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);

    Task<SavePersonGroupResultDto> CreateAsync(string name, CancellationToken cancellationToken = default);

    Task<SavePersonGroupResultDto> RenameAsync(int id, string name, CancellationToken cancellationToken = default);

    /// <summary>Deletes the group and its membership rows; the people are kept. False if it doesn't exist.</summary>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
