using System.Linq.Expressions;
using web.Data.Models;
using web.Repositories.Arrangements.Dtos;
using web.Repositories.Forms.Mappers;

namespace web.Repositories.Arrangements.Mappers;

public static class ArrangementMapper
{
    public static readonly Expression<Func<Arrangement, ArrangementListItemDto>> ToListItemDtoExpression = x => new ArrangementListItemDto
    {
        Id = x.Id,
        Title = x.Title,
        EventDateUtc = x.Shifts.Any() ? x.Shifts.Min(s => s.StartUtc) : null,
        NeededCount = x.Shifts.Sum(s => s.NeededCount),
        RegistrationCount = x.Shifts.Sum(s => s.Registrations.Count),
        RegistrationOpensAtUtc = x.RegistrationOpensAtUtc,
        RegistrationClosesAtUtc = x.RegistrationClosesAtUtc,
        RegistrationForcedOpen = x.RegistrationForcedOpen,
        CreatedAt = x.CreatedAt
    };

    public static ArrangementDto ToDto(this Arrangement entity) => new()
    {
        Id = entity.Id,
        PublicId = entity.PublicId,
        Title = entity.Title,
        Description = entity.Description,
        RegistrationOpensAtUtc = entity.RegistrationOpensAtUtc,
        RegistrationClosesAtUtc = entity.RegistrationClosesAtUtc,
        RegistrationForcedOpen = entity.RegistrationForcedOpen,
        AccessMode = entity.AccessMode,
        AllowMultipleNamesPerShift = entity.AllowMultipleNamesPerShift,
        FormFields = entity.FormFields.OrderBy(f => f.Order).Select(f => new ArrangementFormFieldDto
        {
            Id = f.Id,
            Label = f.Label,
            HelpText = f.HelpText,
            FieldType = f.FieldType,
            IsRequired = f.IsRequired,
            Order = f.Order,
            OptionsJson = f.OptionsJson,
            Options = FormMapper.ParseOptions(f.OptionsJson)
        }).ToList(),
        Shifts = entity.Shifts.OrderBy(s => s.StartUtc).ThenBy(s => s.Order).Select(s => new ArrangementShiftDto
        {
            Id = s.Id,
            Title = s.Title,
            StartUtc = s.StartUtc,
            EndUtc = s.EndUtc,
            Location = s.Location,
            NeededCount = s.NeededCount,
            Order = s.Order,
            Requirements = s.Requirements.OrderBy(r => r.Order)
                .Select(r => new ArrangementShiftRequirementDto { Id = r.Id, Text = r.Text, Order = r.Order })
                .ToList()
        }).ToList(),
        AllowedPersonIds = entity.AllowedPersons.Select(p => p.PersonId).ToList(),
        AllowedGroupIds = entity.AllowedGroups.Select(g => g.PersonGroupId).ToList()
    };

    /// <summary>A new arrangement without fields/shifts — the repository adds them.</summary>
    public static Arrangement ToEntity(this SaveArrangementDto dto) => new() { CreatedByUserId = dto.UserId };

    public static void ApplyTo(this SaveArrangementDto dto, Arrangement entity)
    {
        entity.Title = dto.Title.Trim();
        entity.Description = NullIfBlank(dto.Description);
        entity.RegistrationOpensAtUtc = dto.RegistrationOpensAtUtc;
        entity.RegistrationClosesAtUtc = dto.RegistrationClosesAtUtc;
        entity.AccessMode = dto.AccessMode;
        entity.AllowMultipleNamesPerShift = dto.AllowMultipleNamesPerShift;
    }

    public static void ApplyTo(this SaveArrangementFormFieldDto dto, ArrangementFormField entity, string? optionsJson)
    {
        entity.Label = dto.Label.Trim();
        entity.HelpText = NullIfBlank(dto.HelpText);
        entity.FieldType = dto.FieldType;
        entity.IsRequired = dto.IsRequired;
        entity.Order = dto.Order;
        entity.OptionsJson = optionsJson;
    }

    public static void ApplyTo(this SaveArrangementShiftDto dto, ArrangementShift entity)
    {
        entity.Title = dto.Title.Trim();
        entity.StartUtc = dto.StartUtc;
        entity.EndUtc = dto.EndUtc;
        entity.Location = NullIfBlank(dto.Location);
        entity.NeededCount = dto.NeededCount;
        entity.Order = dto.Order;
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
