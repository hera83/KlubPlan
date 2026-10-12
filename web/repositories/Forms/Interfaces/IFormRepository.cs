using web.Repositories.Forms.Dtos;

namespace web.Repositories.Forms.Interfaces;

/// <summary>
/// Forms and their fields, in version series: a new version is a copy (RootFormId = the first
/// version's id, VersionNumber + 1), and only the latest version of a series can be changed or answered.
/// </summary>
public interface IFormRepository
{
    /// <summary>The latest version of each form series matching the search, newest first; userId drives HasCurrentUserSubmitted.</summary>
    Task<FormPageDto> SearchAsync(SearchFormsDto search, string userId, CancellationToken cancellationToken = default);

    Task<FormDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>The form behind a public /Formular link (Form.PublicId), or null.</summary>
    Task<FormDto?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default);

    /// <summary>The latest version of every form series by title — optionally only those accepting responses (the forms a message can link to).</summary>
    Task<List<FormLinkOptionDto>> GetLatestVersionsAsync(bool acceptingResponsesOnly, CancellationToken cancellationToken = default);

    /// <summary>The latest version in the form's version series (so a resend links to what is live now), or null.</summary>
    Task<FormLinkDto?> GetLatestLinkAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Where the form sits in its version series. Null when not found.</summary>
    Task<FormVersionInfoDto?> GetVersionInfoAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Is this the latest version of its series? False when not found.</summary>
    Task<bool> IsLatestVersionAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Validates and creates/updates the form and its fields. Only the latest version can be updated.</summary>
    Task<SaveFormResultDto> SaveAsync(SaveFormDto dto, CancellationToken cancellationToken = default);

    /// <summary>Opens or closes the form for responses. Only the latest version can be changed.</summary>
    Task<SaveFormResultDto> SetAcceptingResponsesAsync(int id, bool isAcceptingResponses, CancellationToken cancellationToken = default);

    /// <summary>Copies a closed, latest-version form into a new version that accepts responses.</summary>
    Task<CreateFormVersionResultDto> CreateNewVersionAsync(int sourceFormId, string? userId, CancellationToken cancellationToken = default);

    /// <summary>Deletes the form (fields, responses cascade). Deleting a series' first version promotes the next one to root. False when not found.</summary>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
