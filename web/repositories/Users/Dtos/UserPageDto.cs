namespace web.Repositories.Users.Dtos;

public class UserPageDto
{
    public List<UserListItemDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
