namespace web.Repositories.Users.Dtos;

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public int? ProfilePictureId { get; set; }
    public string? ThemePreference { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
