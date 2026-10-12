using web.Constants;
using web.Managers.Forms.Dtos;
using web.Managers.Forms.Interfaces;
using web.Repositories.Forms.Dtos;
using web.Repositories.Forms.Interfaces;
using web.Repositories.FormSubmissions.Dtos;
using web.Repositories.FormSubmissions.Interfaces;
using web.Repositories.People.Interfaces;

namespace web.Managers.Forms;

public class FormManager(
    IFormRepository formRepository,
    IFormSubmissionRepository formSubmissionRepository,
    IPersonRepository personRepository,
    ILogger<FormManager> logger) : IFormManager
{
    private const int DefaultPageSize = 10;
    private const int MinPageSize = 5;
    private const int MaxPageSize = 500;

    public async Task<SubmitFormResultDto> SubmitAsync(int formId, string userId, IReadOnlyList<FormAnswerInputDto> answers, CancellationToken cancellationToken = default)
    {
        var form = await formRepository.GetByIdAsync(formId, cancellationToken);
        if (form is null)
            return new SubmitFormResultDto { Status = SubmitFormStatus.NotFound };
        if (!form.IsAcceptingResponses || !await formRepository.IsLatestVersionAsync(form.Id, cancellationToken))
            return new SubmitFormResultDto { Status = SubmitFormStatus.Closed };

        var (values, fieldErrors) = ValidateAnswers(form, answers);
        if (fieldErrors.Count > 0)
            return new SubmitFormResultDto { Status = SubmitFormStatus.Invalid, FieldErrors = fieldErrors };

        await formSubmissionRepository.CreateAsync(new CreateFormSubmissionDto
        {
            FormId = form.Id,
            SubmittedByUserId = form.IsAnonymous ? null : userId,
            Answers = values
        }, cancellationToken);

        logger.LogInformation("User {UserId} submitted a response to form {FormId}", userId, formId);
        return new SubmitFormResultDto { Status = SubmitFormStatus.Submitted };
    }

    public async Task<PublicFormDto> GetPublicFormAsync(Guid formPublicId, Guid? personPublicId, CancellationToken cancellationToken = default)
    {
        var (status, form, _) = await ResolvePublicAccessAsync(formPublicId, personPublicId, cancellationToken);
        return new PublicFormDto { Status = status, Form = form };
    }

    public async Task<SubmitPublicFormResultDto> SubmitPublicAsync(Guid formPublicId, Guid? personPublicId, IReadOnlyList<FormAnswerInputDto> answers, CancellationToken cancellationToken = default)
    {
        var (status, form, personId) = await ResolvePublicAccessAsync(formPublicId, personPublicId, cancellationToken);
        if (status != PublicFormStatus.Ok || form is null)
            return new SubmitPublicFormResultDto { Status = status };

        var (values, fieldErrors) = ValidateAnswers(form, answers);
        if (fieldErrors.Count > 0)
            return new SubmitPublicFormResultDto { Status = PublicFormStatus.Ok, Success = false, FieldErrors = fieldErrors };

        await formSubmissionRepository.CreateAsync(new CreateFormSubmissionDto
        {
            FormId = form.Id,
            SubmittedByPersonId = personId,
            Answers = values
        }, cancellationToken);

        logger.LogInformation("Person {PersonId} submitted a public response to form {FormId}", personId, form.Id);
        return new SubmitPublicFormResultDto { Status = PublicFormStatus.Ok, Success = true, FormTitle = form.Title };
    }

    public async Task<FormResponsesDto?> GetResponsesAsync(int formId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var responses = await BuildResponsesShellAsync(formId, cancellationToken);
        if (responses is null)
            return null;

        responses.Page = page < 1 ? 1 : page;
        responses.PageSize = pageSize is < MinPageSize or > MaxPageSize ? DefaultPageSize : pageSize;
        responses.TotalCount = await formSubmissionRepository.CountAsync(formId, cancellationToken);
        responses.Rows = await formSubmissionRepository.GetPageAsync(formId, responses.Page, responses.PageSize, cancellationToken);
        return responses;
    }

    public async Task<FormResponsesDto?> GetAllResponsesAsync(int formId, CancellationToken cancellationToken = default)
    {
        var responses = await BuildResponsesShellAsync(formId, cancellationToken);
        if (responses is null)
            return null;

        responses.TotalCount = await formSubmissionRepository.CountAsync(formId, cancellationToken);
        responses.Page = 1;
        responses.PageSize = responses.TotalCount == 0 ? 1 : responses.TotalCount;
        responses.Rows = await formSubmissionRepository.GetPageAsync(formId, 1, responses.PageSize, cancellationToken);
        return responses;
    }

    private async Task<FormResponsesDto?> BuildResponsesShellAsync(int formId, CancellationToken cancellationToken)
    {
        var form = await formRepository.GetByIdAsync(formId, cancellationToken);
        if (form is null)
            return null;

        return new FormResponsesDto
        {
            FormId = form.Id,
            FormTitle = form.Title,
            Columns = form.Fields
                .Where(f => FormFieldTypes.IsAnswerable(f.FieldType))
                .Select(f => new FormResponseColumnDto { FormFieldId = f.Id, Label = f.Label })
                .ToList(),
            VersionInfo = (await formRepository.GetVersionInfoAsync(form.Id, cancellationToken))!
        };
    }

    /// <summary>
    /// Never resolves a person for an anonymous form — identity is never recorded for those,
    /// whatever the link contains.
    /// </summary>
    private async Task<(PublicFormStatus Status, FormDto? Form, int? PersonId)> ResolvePublicAccessAsync(Guid formPublicId, Guid? personPublicId, CancellationToken cancellationToken)
    {
        var form = await formRepository.GetByPublicIdAsync(formPublicId, cancellationToken);
        if (form is null)
            return (PublicFormStatus.NotFound, null, null);
        if (!form.IsAcceptingResponses || !await formRepository.IsLatestVersionAsync(form.Id, cancellationToken))
            return (PublicFormStatus.Closed, form, null);
        if (form.IsAnonymous)
            return (PublicFormStatus.Ok, form, null);
        if (personPublicId is null || personPublicId == Guid.Empty)
            return (PublicFormStatus.MissingIdentity, form, null);

        var personId = await personRepository.GetIdByPublicIdAsync(personPublicId.Value, cancellationToken);
        if (personId is null)
            return (PublicFormStatus.InvalidIdentity, form, null);

        return await formSubmissionRepository.HasPersonSubmittedAsync(form.Id, personId.Value, cancellationToken)
            ? (PublicFormStatus.AlreadySubmitted, form, personId)
            : (PublicFormStatus.Ok, form, personId);
    }

    /// <summary>Checks the posted answers against the form's answerable fields: required fields must be filled; checkbox values are joined with ", ".</summary>
    private static (List<FormAnswerValueDto> Values, Dictionary<int, string> FieldErrors) ValidateAnswers(FormDto form, IReadOnlyList<FormAnswerInputDto> answers)
    {
        var answersByField = answers.ToDictionary(a => a.FormFieldId);
        var fieldErrors = new Dictionary<int, string>();
        var values = new List<FormAnswerValueDto>();

        foreach (var field in form.Fields.Where(f => FormFieldTypes.IsAnswerable(f.FieldType)))
        {
            answersByField.TryGetValue(field.Id, out var answer);
            var isCheckboxes = FormFieldTypes.AllowsMultipleValues(field.FieldType);

            var multiple = isCheckboxes
                ? (answer?.Values ?? []).Where(v => !string.IsNullOrWhiteSpace(v)).ToList()
                : [];
            var single = isCheckboxes ? null : answer?.Value?.Trim();

            var isEmpty = isCheckboxes ? multiple.Count == 0 : string.IsNullOrWhiteSpace(single);
            if (field.IsRequired && isEmpty)
            {
                fieldErrors[field.Id] = "Dette felt er påkrævet.";
                continue;
            }

            values.Add(new FormAnswerValueDto
            {
                FormFieldId = field.Id,
                ValueText = isCheckboxes ? (multiple.Count > 0 ? string.Join(", ", multiple) : null) : single
            });
        }

        return (values, fieldErrors);
    }
}
