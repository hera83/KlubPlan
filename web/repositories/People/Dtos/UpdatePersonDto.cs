using web.Constants;

namespace web.Repositories.People.Dtos;

/// <summary>Replaces all of a person's fields, memberships and guardians.</summary>
public class UpdatePersonDto
{
    /// <summary>Club member number. Blank = the next free sequential number (zero-padded to 6 digits).</summary>
    public string? Uid { get; set; }

    public string Name { get; set; } = string.Empty;
    public DateOnly? BirthDate { get; set; }
    public string? Mobile { get; set; }
    public string? Email { get; set; }
    public List<int> GroupIds { get; set; } = [];

    /// <summary>Role per group, keyed by GroupId. A group without an entry gets <see cref="PersonType.Player"/>.</summary>
    public Dictionary<int, PersonType> GroupTypes { get; set; } = [];

    /// <summary>Guardians in display order. Entries with a blank name are skipped.</summary>
    public List<PersonGuardianDto> Guardians { get; set; } = [];
}
