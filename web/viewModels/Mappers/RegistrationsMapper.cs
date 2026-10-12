using web.Constants;
using web.Extensions;
using web.Managers.Arrangements.Dtos;
using web.Repositories.ArrangementRegistrations.Dtos;
using web.Repositories.Arrangements.Dtos;

namespace web.ViewModels.Mappers;

/// <summary>
/// Maps between the Registrations/Tilmelding view models and the Arrangements repository's and
/// manager's Dtos. The builder's datetime-local fields are local time; the Dtos are UTC.
/// </summary>
public static class RegistrationsMapper
{
    public static SearchArrangementsDto ToSearchArrangementsDto(this ArrangementFilterViewModel filter) => new()
    {
        SearchText = filter.SearchText,
        Page = filter.Page,
        PageSize = filter.PageSize
    };

    public static ArrangementFilterViewModel ToArrangementFilterViewModel(this ArrangementPageDto page, ArrangementFilterViewModel filter) => new()
    {
        SearchText = filter.SearchText,
        Page = page.Page,
        PageSize = page.PageSize,
        TotalCount = page.TotalCount,
        Arrangementer = page.Items.Select(a => new ArrangementListItemViewModel
        {
            Id = a.Id,
            Title = a.Title,
            EventDateUtc = a.EventDateUtc,
            RegistrationCount = a.RegistrationCount,
            MissingCount = Math.Max(0, a.NeededCount - a.RegistrationCount),
            RegistrationOpensAtUtc = a.RegistrationOpensAtUtc,
            RegistrationClosesAtUtc = a.RegistrationClosesAtUtc,
            RegistrationForcedOpen = a.RegistrationForcedOpen,
            CreatedAtUtc = a.CreatedAt
        }).ToList()
    };

    public static ArrangementBuilderViewModel ToArrangementBuilderViewModel(this ArrangementBuilderDto builder)
    {
        var model = new ArrangementBuilderViewModel();
        if (builder.Arrangement is { } a)
        {
            model.Id = a.Id;
            model.Title = a.Title;
            model.Description = a.Description;
            model.FormFields = a.FormFields.Select(f => new ArrangementFormFieldBuilderViewModel
            {
                Id = f.Id,
                Label = f.Label,
                HelpText = f.HelpText,
                FieldType = f.FieldType,
                IsRequired = f.IsRequired,
                Order = f.Order,
                OptionsJson = f.OptionsJson
            }).ToList();
            model.Shifts = a.Shifts.Select(s => new ArrangementShiftBuilderViewModel
            {
                Id = s.Id,
                Title = s.Title,
                Start = s.StartUtc.FromUtcToLocal(),
                End = s.EndUtc.FromUtcToLocal(),
                Location = s.Location,
                NeededCount = s.NeededCount,
                Order = s.Order,
                Requirements = s.Requirements
                    .Select(r => new ArrangementShiftRequirementBuilderViewModel { Id = r.Id, Text = r.Text, Order = r.Order })
                    .ToList()
            }).ToList();
            model.RegistrationOpensAt = a.RegistrationOpensAtUtc.FromUtcToLocal();
            model.RegistrationClosesAt = a.RegistrationClosesAtUtc.FromUtcToLocal();
            model.AccessMode = a.AccessMode;
            model.AllowedPersonIds = a.AllowedPersonIds;
            model.AllowedGroupIds = a.AllowedGroupIds;
            model.AllowMultipleNamesPerShift = a.AllowMultipleNamesPerShift;
        }

        model.ApplyOptions(builder);
        return model;
    }

    /// <summary>Fills the access tab's option lists (also when re-showing a builder after a failed save).</summary>
    public static void ApplyOptions(this ArrangementBuilderViewModel model, ArrangementBuilderDto builder)
    {
        model.GroupOptions = builder.Groups.Select(g => new PersonGroupOptionViewModel { Id = g.Id, Name = g.Name }).ToList();
        model.PersonOptions = builder.People.Select(p => new ArrangementPersonOptionViewModel { Id = p.Id, Uid = p.Uid, Name = p.Name }).ToList();
    }

    public static SaveArrangementDto ToSaveArrangementDto(this ArrangementBuilderViewModel model, string? userId) => new()
    {
        Id = model.Id,
        Title = model.Title,
        Description = model.Description,
        UserId = userId,
        FormFields = model.FormFields.Select(f => new SaveArrangementFormFieldDto
        {
            Id = f.Id,
            Label = f.Label,
            HelpText = f.HelpText,
            FieldType = f.FieldType,
            IsRequired = f.IsRequired,
            Order = f.Order,
            OptionsJson = f.OptionsJson
        }).ToList(),
        Shifts = model.Shifts.Select(s => new SaveArrangementShiftDto
        {
            Id = s.Id,
            Title = s.Title,
            StartUtc = s.Start.FromLocalToUtc(),
            EndUtc = s.End.FromLocalToUtc(),
            Location = s.Location,
            NeededCount = s.NeededCount,
            Order = s.Order,
            Requirements = s.Requirements.Select(r => new SaveArrangementShiftRequirementDto { Id = r.Id, Text = r.Text, Order = r.Order }).ToList()
        }).ToList(),
        RegistrationOpensAtUtc = model.RegistrationOpensAt.FromLocalToUtc(),
        RegistrationClosesAtUtc = model.RegistrationClosesAt.FromLocalToUtc(),
        AccessMode = model.AccessMode,
        AllowedPersonIds = model.AllowedPersonIds,
        AllowedGroupIds = model.AllowedGroupIds,
        AllowMultipleNamesPerShift = model.AllowMultipleNamesPerShift
    };

    public static PublicArrangementAccessViewModel ToPublicArrangementAccessViewModel(this PublicArrangementDto publicArrangement, Guid? personPublicId)
    {
        var model = new PublicArrangementAccessViewModel { Status = publicArrangement.Status };
        if (publicArrangement.Arrangement is not { } a)
            return model;

        if (publicArrangement.Status != PublicArrangementStatus.Ok)
        {
            model.Arrangement = new ArrangementFillViewModel
            {
                ArrangementId = a.Id,
                ArrangementPublicId = a.PublicId,
                Title = a.Title,
                Description = a.Description,
                PersonPublicId = personPublicId,
                RegistrationOpensAtUtc = a.RegistrationOpensAtUtc
            };
            return model;
        }

        model.Arrangement = new ArrangementFillViewModel
        {
            ArrangementId = a.Id,
            ArrangementPublicId = a.PublicId,
            Title = a.Title,
            Description = a.Description,
            PersonPublicId = personPublicId,
            PersonName = publicArrangement.PersonName,
            AllowMultipleNamesPerShift = a.AllowMultipleNamesPerShift,
            Fields = a.FormFields.Select(f => new ArrangementFieldFillViewModel
            {
                ArrangementFormFieldId = f.Id,
                Label = f.Label,
                HelpText = f.HelpText,
                FieldType = f.FieldType,
                IsRequired = f.IsRequired,
                Options = f.Options
            }).ToList(),
            // Full shifts are left out — nothing to sign up for.
            Shifts = a.Shifts
                .Select(s => new ArrangementShiftFillViewModel
                {
                    Id = s.Id,
                    Title = s.Title,
                    Start = s.StartUtc.FromUtcToLocal(),
                    End = s.EndUtc.FromUtcToLocal(),
                    Location = s.Location,
                    RemainingCount = Math.Max(0, s.NeededCount - publicArrangement.TakenCountByShift.GetValueOrDefault(s.Id)),
                    Requirements = s.Requirements.Select(r => new ArrangementShiftRequirementFillViewModel { Id = r.Id, Text = r.Text }).ToList()
                })
                .Where(s => s.RemainingCount > 0)
                .ToList()
        };
        return model;
    }

    public static SubmitPublicArrangementDto ToSubmitPublicArrangementDto(
        this List<ArrangementAnswerInputViewModel>? answers,
        List<int>? shiftIds,
        List<int>? confirmedRequirementIds,
        List<ArrangementShiftCompanionInputViewModel>? companions) => new()
    {
        Answers = (answers ?? []).Select(a => new ArrangementAnswerInputDto { ArrangementFormFieldId = a.ArrangementFormFieldId, Value = a.Value, Values = a.Values }).ToList(),
        SelectedShiftIds = shiftIds ?? [],
        ConfirmedRequirementIds = confirmedRequirementIds ?? [],
        Companions = (companions ?? []).Select(c => new ArrangementCompanionInputDto { ShiftId = c.ShiftId, Name = c.Name }).ToList()
    };

    /// <summary>Keeps what the registrant already entered when the sign-up form is shown again with errors.</summary>
    public static void ApplySubmitted(this ArrangementFillViewModel arrangement, SubmitPublicArrangementDto input)
    {
        var answers = input.Answers.ToDictionary(a => a.ArrangementFormFieldId);
        foreach (var field in arrangement.Fields)
        {
            if (!answers.TryGetValue(field.ArrangementFormFieldId, out var answer)) continue;
            field.SubmittedValue = answer.Value;
            field.SubmittedValues = answer.Values;
        }

        foreach (var shift in arrangement.Shifts)
        {
            shift.IsSelected = input.SelectedShiftIds.Contains(shift.Id);
            shift.SubmittedCompanionNames = input.Companions.Where(c => c.ShiftId == shift.Id).Select(c => c.Name ?? string.Empty).ToList();
        }
    }

    public static ArrangementRegistrationsViewModel ToArrangementRegistrationsViewModel(this ArrangementRegistrationsDto registrations) => new()
    {
        ArrangementId = registrations.ArrangementId,
        ArrangementTitle = registrations.ArrangementTitle,
        Columns = registrations.Columns.Select(c => new ArrangementRegistrationColumnViewModel { ArrangementFormFieldId = c.ArrangementFormFieldId, Label = c.Label }).ToList(),
        Page = registrations.Page,
        PageSize = registrations.PageSize,
        TotalCount = registrations.TotalCount,
        Rows = registrations.Rows.Select(r => new ArrangementRegistrationRowViewModel
        {
            RegistrationId = r.Id,
            PersonName = r.PersonName,
            RegisteredAtUtc = r.RegisteredAtUtc,
            Answers = r.Answers,
            ShiftLabels = ToShiftLabels(r.Shifts)
        }).ToList()
    };

    /// <summary>One label per shift, "Titel (dd/MM HH:mm–HH:mm)", followed by the named slots.</summary>
    private static List<string> ToShiftLabels(List<RegistrationShiftDto> shifts) => shifts
        .GroupBy(s => s.ArrangementShiftId)
        .OrderBy(g => g.First().StartUtc)
        .Select(g =>
        {
            var shift = g.First();
            var label = $"{shift.Title} ({shift.StartUtc.FromUtcToLocal():dd/MM HH:mm}–{shift.EndUtc.FromUtcToLocal():HH:mm})";
            var names = g.Select(s => s.CompanionName).Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
            if (names.Count == 0) return label;

            // A null CompanionName row means the registrant is still one of the slots — "+" reads as
            // "plus these". Once every slot is explicitly named, list the names as-is.
            var hasImplicitSelf = g.Any(s => s.CompanionName is null);
            return hasImplicitSelf ? $"{label} — + {string.Join(", ", names)}" : $"{label} — {string.Join(", ", names)}";
        })
        .ToList();
}
