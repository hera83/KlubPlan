using web.Constants;

namespace web.Repositories.People.Dtos;

public class SearchPeopleDto
{
    /// <summary>Case-insensitive substring of name, UID, mobile or email.</summary>
    public string? SearchText { get; set; }

    /// <summary>Only people in at least one of these groups. Empty = all.</summary>
    public List<int> GroupIds { get; set; } = [];

    /// <summary>Only people who have this role in at least one of their memberships.</summary>
    public PersonType? Type { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
