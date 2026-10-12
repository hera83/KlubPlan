using web.Managers.Arrangements.Dtos;

namespace web.Managers.Arrangements.Interfaces;

/// <summary>Arrangement operations across Arrangements, ArrangementRegistrations, People and PersonGroups.</summary>
public interface IArrangementManager
{
    /// <summary>The builder's data: the arrangement (null id = create) and the person/group option lists. Null when the id isn't found.</summary>
    Task<ArrangementBuilderDto?> GetBuilderAsync(int? id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gate for the public, unauthenticated /Tilmelding link: the arrangement must exist and be open
    /// for registration, the UId must be a real person (there is no anonymous mode), allowed by the
    /// access list when restricted, and not already registered.
    /// </summary>
    Task<PublicArrangementDto> GetPublicAsync(Guid arrangementPublicId, Guid? personPublicId, CancellationToken cancellationToken = default);

    /// <summary>A sign-up via the public link: gate, answers, shift selection, capacity and requirements are all checked before saving.</summary>
    Task<SubmitPublicArrangementResultDto> SubmitPublicAsync(Guid arrangementPublicId, Guid? personPublicId, SubmitPublicArrangementDto input, CancellationToken cancellationToken = default);

    /// <summary>One page of the registrations (page size 5-500, otherwise 10). Null when the arrangement doesn't exist.</summary>
    Task<ArrangementRegistrationsDto?> GetRegistrationsAsync(int arrangementId, int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>All registrations on one page (for CSV export). Null when the arrangement doesn't exist.</summary>
    Task<ArrangementRegistrationsDto?> GetAllRegistrationsAsync(int arrangementId, CancellationToken cancellationToken = default);
}
