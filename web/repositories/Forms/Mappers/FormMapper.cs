using System.Text.Json;
using web.Data.Models;
using web.Repositories.Forms.Dtos;

namespace web.Repositories.Forms.Mappers;

public static class FormMapper
{
    public static FormDto ToDto(this Form entity) => new()
    {
        Id = entity.Id,
        PublicId = entity.PublicId,
        Title = entity.Title,
        Description = entity.Description,
        IsAcceptingResponses = entity.IsAcceptingResponses,
        IsAnonymous = entity.IsAnonymous,
        VersionNumber = entity.VersionNumber,
        RootFormId = entity.RootFormId,
        Fields = entity.Fields.OrderBy(f => f.Order).Select(ToDto).ToList()
    };

    public static FormFieldDto ToDto(this FormField entity) => new()
    {
        Id = entity.Id,
        Label = entity.Label,
        HelpText = entity.HelpText,
        FieldType = entity.FieldType,
        IsRequired = entity.IsRequired,
        Order = entity.Order,
        OptionsJson = entity.OptionsJson,
        Options = ParseOptions(entity.OptionsJson)
    };

    /// <summary>A new form (version 1) without fields — the repository adds them.</summary>
    public static Form ToEntity(this SaveFormDto dto) => new() { CreatedByUserId = dto.UserId };

    public static void ApplyTo(this SaveFormDto dto, Form entity)
    {
        entity.Title = dto.Title.Trim();
        entity.Description = NullIfBlank(dto.Description);
        entity.IsAcceptingResponses = dto.IsAcceptingResponses;
        entity.IsAnonymous = dto.IsAnonymous;
    }

    public static FormField ToEntity(this SaveFormFieldDto dto, string? optionsJson)
    {
        var field = new FormField();
        dto.ApplyTo(field, optionsJson);
        return field;
    }

    public static void ApplyTo(this SaveFormFieldDto dto, FormField entity, string? optionsJson)
    {
        entity.Label = dto.Label.Trim();
        entity.HelpText = NullIfBlank(dto.HelpText);
        entity.FieldType = dto.FieldType;
        entity.IsRequired = dto.IsRequired;
        entity.Order = dto.Order;
        entity.OptionsJson = optionsJson;
    }

    /// <summary>The options of a choice field: the JSON array parsed, trimmed and without blank entries.</summary>
    public static List<string> ParseOptions(string? optionsJson)
    {
        if (string.IsNullOrWhiteSpace(optionsJson)) return [];

        try
        {
            var options = JsonSerializer.Deserialize<List<string>>(optionsJson) ?? [];
            return options
                .Select(o => o?.Trim() ?? string.Empty)
                .Where(o => !string.IsNullOrWhiteSpace(o))
                .ToList();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
