namespace web.Repositories.People.Dtos;

/// <summary>One person with group memberships and guardian contacts.</summary>
public class PersonDto
{
    public int Id { get; set; }
    public string Uid { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateOnly? BirthDate { get; set; }
    public string? Mobile { get; set; }
    public string? Email { get; set; }
    public List<PersonMembershipDto> Memberships { get; set; } = [];
    public List<PersonGuardianDto> Guardians { get; set; } = [];
}
