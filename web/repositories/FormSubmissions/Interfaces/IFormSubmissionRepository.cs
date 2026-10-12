using web.Repositories.FormSubmissions.Dtos;

namespace web.Repositories.FormSubmissions.Interfaces;

/// <summary>Responses to forms (FormSubmission) and their answers (FormAnswer).</summary>
public interface IFormSubmissionRepository
{
    /// <summary>Saves an already validated response. Returns the new submission's id.</summary>
    Task<int> CreateAsync(CreateFormSubmissionDto dto, CancellationToken cancellationToken = default);

    Task<int> CountAsync(int formId, CancellationToken cancellationToken = default);

    /// <summary>One page of the form's responses, newest first, with who answered resolved to a name.</summary>
    Task<List<FormSubmissionDto>> GetPageAsync(int formId, int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>Has this person already answered the form (via a public link)?</summary>
    Task<bool> HasPersonSubmittedAsync(int formId, int personId, CancellationToken cancellationToken = default);
}
