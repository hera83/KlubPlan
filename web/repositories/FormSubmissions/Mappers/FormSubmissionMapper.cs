using web.Data.Models;
using web.Repositories.FormSubmissions.Dtos;

namespace web.Repositories.FormSubmissions.Mappers;

public static class FormSubmissionMapper
{
    public static FormSubmission ToEntity(this CreateFormSubmissionDto dto, DateTime submittedAtUtc)
    {
        var submission = new FormSubmission
        {
            FormId = dto.FormId,
            SubmittedByUserId = dto.SubmittedByUserId,
            SubmittedByPersonId = dto.SubmittedByPersonId,
            SubmittedAtUtc = submittedAtUtc
        };

        foreach (var answer in dto.Answers)
            submission.Answers.Add(new FormAnswer { FormFieldId = answer.FormFieldId, ValueText = answer.ValueText });

        return submission;
    }

    /// <summary>"Who answered": the user's name, the person's name, or "Anonym" for anonymous forms.</summary>
    public static FormSubmissionDto ToDto(this FormSubmission entity, IReadOnlyDictionary<string, string> userNames, IReadOnlyDictionary<int, string> personNames) => new()
    {
        Id = entity.Id,
        SubmittedByDisplayName = entity.SubmittedByUserId is not null
            ? userNames.GetValueOrDefault(entity.SubmittedByUserId, "Ukendt bruger")
            : entity.SubmittedByPersonId is not null
                ? personNames.GetValueOrDefault(entity.SubmittedByPersonId.Value, "Ukendt person")
                : "Anonym",
        SubmittedAtUtc = entity.SubmittedAtUtc,
        Answers = entity.Answers.ToDictionary(a => a.FormFieldId, a => a.ValueText ?? string.Empty)
    };
}
