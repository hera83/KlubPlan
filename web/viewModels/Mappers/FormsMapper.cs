using web.Constants;
using web.Managers.Forms.Dtos;
using web.Repositories.Forms.Dtos;

namespace web.ViewModels.Mappers;

/// <summary>Maps between the Forms/Formular view models and the Forms repository's and manager's Dtos.</summary>
public static class FormsMapper
{
    public static SearchFormsDto ToSearchFormsDto(this FormFilterViewModel filter) => new()
    {
        SearchText = filter.SearchText,
        Page = filter.Page,
        PageSize = filter.PageSize
    };

    public static FormFilterViewModel ToFormFilterViewModel(this FormPageDto page, FormFilterViewModel filter, bool isAdmin) => new()
    {
        SearchText = filter.SearchText,
        Page = page.Page,
        PageSize = page.PageSize,
        TotalCount = page.TotalCount,
        IsAdmin = isAdmin,
        Forms = page.Items.Select(f => new FormListItemViewModel
        {
            Id = f.Id,
            PublicId = f.PublicId,
            Title = f.Title,
            Description = f.Description,
            IsAcceptingResponses = f.IsAcceptingResponses,
            IsAnonymous = f.IsAnonymous,
            CreatedAtUtc = f.CreatedAt,
            UpdatedAtUtc = f.UpdatedAt,
            FieldCount = f.FieldCount,
            ResponseCount = f.ResponseCount,
            HasCurrentUserSubmitted = f.HasCurrentUserSubmitted,
            VersionNumber = f.VersionNumber,
            VersionCount = f.VersionCount,
            Versions = f.Versions.Select(v => new FormVersionOptionViewModel
            {
                Id = v.Id,
                VersionNumber = v.VersionNumber,
                CreatedAtUtc = v.CreatedAt,
                IsAcceptingResponses = v.IsAcceptingResponses,
                IsCurrent = v.IsCurrent
            }).ToList()
        }).ToList()
    };

    public static FormBuilderViewModel ToFormBuilderViewModel(this FormDto form, FormVersionInfoDto versionInfo) => new()
    {
        Id = form.Id,
        Title = form.Title,
        Description = form.Description,
        IsAcceptingResponses = form.IsAcceptingResponses,
        IsAnonymous = form.IsAnonymous,
        Fields = form.Fields.Select(f => new FormFieldBuilderViewModel
        {
            Id = f.Id,
            Label = f.Label,
            HelpText = f.HelpText,
            FieldType = f.FieldType,
            IsRequired = f.IsRequired,
            Order = f.Order,
            OptionsJson = f.OptionsJson
        }).ToList(),
        VersionNumber = versionInfo.VersionNumber,
        VersionCount = versionInfo.VersionCount,
        IsLatestVersion = versionInfo.IsLatestVersion,
        CurrentVersionId = versionInfo.CurrentVersionId,
        PreviousVersionId = versionInfo.PreviousVersionId,
        NextVersionId = versionInfo.NextVersionId
    };

    public static SaveFormDto ToSaveFormDto(this FormBuilderViewModel model, string? userId) => new()
    {
        Id = model.Id,
        Title = model.Title,
        Description = model.Description,
        IsAcceptingResponses = model.IsAcceptingResponses,
        IsAnonymous = model.IsAnonymous,
        UserId = userId,
        Fields = model.Fields.Select(f => new SaveFormFieldDto
        {
            Id = f.Id,
            Label = f.Label,
            HelpText = f.HelpText,
            FieldType = f.FieldType,
            IsRequired = f.IsRequired,
            Order = f.Order,
            OptionsJson = f.OptionsJson
        }).ToList()
    };

    /// <summary>The fill-out form. isAcceptingResponses decides whether it can be answered (closed or old version: read-only).</summary>
    public static FormFillViewModel ToFormFillViewModel(this FormDto form, bool isAcceptingResponses, Guid? personPublicId = null, bool includeFields = true) => new()
    {
        FormId = form.Id,
        FormPublicId = form.PublicId,
        Title = form.Title,
        Description = form.Description,
        IsAcceptingResponses = isAcceptingResponses,
        IsAnonymous = form.IsAnonymous,
        PersonPublicId = personPublicId,
        Fields = includeFields
            ? form.Fields.Select(f => new FormFieldFillViewModel
            {
                FormFieldId = f.Id,
                Label = f.Label,
                HelpText = f.HelpText,
                FieldType = f.FieldType,
                IsRequired = f.IsRequired,
                Options = f.Options
            }).ToList()
            : []
    };

    public static PublicFormAccessViewModel ToPublicFormAccessViewModel(this PublicFormDto publicForm, Guid? personPublicId)
    {
        var isOk = publicForm.Status == PublicFormStatus.Ok;
        return new PublicFormAccessViewModel
        {
            Status = publicForm.Status,
            // Fields are only shown when the gate is open.
            Form = publicForm.Form?.ToFormFillViewModel(isOk, personPublicId, includeFields: isOk)
        };
    }

    public static List<FormAnswerInputDto> ToFormAnswerInputDtos(this List<FormAnswerInputViewModel>? answers) =>
        (answers ?? []).Select(a => new FormAnswerInputDto { FormFieldId = a.FormFieldId, Value = a.Value, Values = a.Values }).ToList();

    /// <summary>Keeps what the respondent already typed when the form is shown again with errors.</summary>
    public static void ApplySubmittedAnswers(this FormFillViewModel form, IReadOnlyList<FormAnswerInputDto> answers)
    {
        var byField = answers.ToDictionary(a => a.FormFieldId);
        foreach (var field in form.Fields)
        {
            if (!byField.TryGetValue(field.FormFieldId, out var answer)) continue;
            field.SubmittedValue = answer.Value;
            field.SubmittedValues = answer.Values;
        }
    }

    public static FormResponsesViewModel ToFormResponsesViewModel(this FormResponsesDto responses) => new()
    {
        FormId = responses.FormId,
        FormTitle = responses.FormTitle,
        Columns = responses.Columns.Select(c => new FormResponseColumnViewModel { FormFieldId = c.FormFieldId, Label = c.Label }).ToList(),
        Page = responses.Page,
        PageSize = responses.PageSize,
        TotalCount = responses.TotalCount,
        Rows = responses.Rows.Select(r => new FormResponseRowViewModel
        {
            SubmissionId = r.Id,
            SubmittedByDisplayName = r.SubmittedByDisplayName,
            SubmittedAtUtc = r.SubmittedAtUtc,
            Answers = r.Answers
        }).ToList(),
        VersionNumber = responses.VersionInfo.VersionNumber,
        VersionCount = responses.VersionInfo.VersionCount,
        IsLatestVersion = responses.VersionInfo.IsLatestVersion,
        CurrentVersionId = responses.VersionInfo.CurrentVersionId,
        PreviousVersionId = responses.VersionInfo.PreviousVersionId,
        NextVersionId = responses.VersionInfo.NextVersionId
    };
}
