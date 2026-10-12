namespace web.Repositories.People.Dtos;

/// <summary>A person in a picker list.</summary>
public class PersonOptionDto
{
    public int Id { get; set; }
    public string Uid { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
