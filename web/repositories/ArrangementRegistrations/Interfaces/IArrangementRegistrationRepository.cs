using web.Repositories.ArrangementRegistrations.Dtos;

namespace web.Repositories.ArrangementRegistrations.Interfaces;

/// <summary>Sign-ups for arrangements: the registration, its answers and the shift slots it takes.</summary>
public interface IArrangementRegistrationRepository
{
    /// <summary>Saves an already validated registration. Returns the new registration's id.</summary>
    Task<int> CreateAsync(CreateArrangementRegistrationDto dto, CancellationToken cancellationToken = default);

    Task<int> CountAsync(int arrangementId, CancellationToken cancellationToken = default);

    /// <summary>One page of the arrangement's registrations, newest first.</summary>
    Task<List<ArrangementRegistrationDto>> GetPageAsync(int arrangementId, int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>Has the person already registered for the arrangement?</summary>
    Task<bool> ExistsForPersonAsync(int arrangementId, int personId, CancellationToken cancellationToken = default);

    /// <summary>Taken slots per shift id (shifts without registrations are left out).</summary>
    Task<Dictionary<int, int>> GetTakenCountsAsync(IReadOnlyCollection<int> shiftIds, CancellationToken cancellationToken = default);
}
