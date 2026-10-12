namespace web.Repositories.People.Dtos;

/// <summary>One page of the people list. Page/PageSize are the values actually used (after clamping).</summary>
public class PersonPageDto
{
    public List<PersonListItemDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
