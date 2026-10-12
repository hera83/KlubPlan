namespace web.Repositories.PersonGroups.Dtos;

public class PersonGroupDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MemberCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
