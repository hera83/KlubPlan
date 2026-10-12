namespace web.Repositories.People.Dtos;

/// <summary>A person's own contact info plus their guardians' — used to address messages.</summary>
public class PersonContactDto
{
    public int Id { get; set; }
    public Guid PublicId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Mobile { get; set; }
    public List<PersonContactGuardianDto> Guardians { get; set; } = [];
}
