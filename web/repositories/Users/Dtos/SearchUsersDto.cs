namespace web.Repositories.Users.Dtos;

public class SearchUsersDto
{
    /// <summary>Matches display name or e-mail.</summary>
    public string? SearchText { get; set; }
    public string? Role { get; set; }
    public bool? IsActive { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
