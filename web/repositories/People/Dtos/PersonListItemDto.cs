namespace web.Repositories.People.Dtos;

/// <summary>A person as shown in the people list: memberships ordered by group name and a guardian count.</summary>
public class PersonListItemDto
{
    public int Id { get; set; }
    public string Uid { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateOnly? BirthDate { get; set; }
    public string? Mobile { get; set; }
    public string? Email { get; set; }
    public List<PersonMembershipDto> Memberships { get; set; } = [];
    public int GuardianCount { get; set; }
}
