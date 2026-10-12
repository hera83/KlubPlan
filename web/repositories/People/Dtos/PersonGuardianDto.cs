namespace web.Repositories.People.Dtos;

/// <summary>A guardian contact on a person. Id is null for a guardian that hasn't been saved yet.</summary>
public class PersonGuardianDto
{
    public int? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public string? Email { get; set; }
}
