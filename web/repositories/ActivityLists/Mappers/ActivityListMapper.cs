using web.Data.Models;
using web.Repositories.ActivityLists.Dtos;

namespace web.Repositories.ActivityLists.Mappers;

/// <summary>
/// The list repository mostly projects straight into its Dtos (counts and sub-queries), so the
/// mapper only holds what is shared: columns and the stored choice options.
/// </summary>
public static class ActivityListMapper
{
    public static ActivityListColumnDto ToDto(this ActivityListColumn entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Kind = entity.Kind,
        Options = ParseOptions(entity.Options),
        IsHidden = entity.IsHidden
    };

    /// <summary>A Choice column's options: one per line, trimmed, no duplicates (case-insensitive), at most 50.</summary>
    public static List<string> ParseOptions(string? options)
        => (options ?? string.Empty)
            .Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(50)
            .ToList();
}
