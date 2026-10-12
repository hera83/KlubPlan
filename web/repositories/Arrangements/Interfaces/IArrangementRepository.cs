using web.Repositories.Arrangements.Dtos;

namespace web.Repositories.Arrangements.Interfaces;

/// <summary>
/// Arrangementer (Tilmeldinger): an arrangement with its own sign-up form fields, shifts with
/// per-shift requirements, a registration window and an access list (persons/groups).
/// </summary>
public interface IArrangementRepository
{
    /// <summary>The table page matching the search, newest first (page size 5-200, otherwise 10).</summary>
    Task<ArrangementPageDto> SearchAsync(SearchArrangementsDto search, CancellationToken cancellationToken = default);

    /// <summary>All arrangements by title with access rules and registration window — the arrangements a message can link to.</summary>
    Task<List<ArrangementLinkOptionDto>> GetLinkOptionsAsync(CancellationToken cancellationToken = default);

    Task<ArrangementDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>The arrangement behind a public /Tilmelding link (Arrangement.PublicId), or null.</summary>
    Task<ArrangementDto?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default);

    /// <summary>Validates and creates/updates the arrangement with its fields, shifts, requirements and access list.</summary>
    Task<SaveArrangementResultDto> SaveAsync(SaveArrangementDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Flips RegistrationForcedOpen: closed → temporarily forced open (overriding the dates), forced open
    /// → back to the dates. Returns the new state, or null when not found.
    /// </summary>
    Task<bool?> ToggleRegistrationForcedOpenAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Deletes the arrangement with everything under it (incl. registrations). False when not found.</summary>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
