using web.Managers.Forms.Dtos;

namespace web.Managers.Forms.Interfaces;

/// <summary>Answering forms and reading the answers: combines Forms, FormSubmissions and (for public links) People.</summary>
public interface IFormManager
{
    /// <summary>A logged-in user's response. The user isn't recorded when the form is anonymous.</summary>
    Task<SubmitFormResultDto> SubmitAsync(int formId, string userId, IReadOnlyList<FormAnswerInputDto> answers, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gate for the public, unauthenticated /Formular link: the form must exist, be open and be the
    /// latest version; unless it is anonymous, the UId must be a real person who hasn't answered yet.
    /// </summary>
    Task<PublicFormDto> GetPublicFormAsync(Guid formPublicId, Guid? personPublicId, CancellationToken cancellationToken = default);

    /// <summary>A response via the public link — the gate is checked again before saving.</summary>
    Task<SubmitPublicFormResultDto> SubmitPublicAsync(Guid formPublicId, Guid? personPublicId, IReadOnlyList<FormAnswerInputDto> answers, CancellationToken cancellationToken = default);

    /// <summary>One page of the form's responses (page size 5-500, otherwise 10). Null when the form doesn't exist.</summary>
    Task<FormResponsesDto?> GetResponsesAsync(int formId, int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>All the form's responses on one page (for CSV export). Null when the form doesn't exist.</summary>
    Task<FormResponsesDto?> GetAllResponsesAsync(int formId, CancellationToken cancellationToken = default);
}
