using web.Constants;

namespace web.Repositories.People.Dtos;

/// <summary>A person's membership of one group, with the role they have in that group.</summary>
public class PersonMembershipDto
{
    public int GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public PersonType Type { get; set; }
}
