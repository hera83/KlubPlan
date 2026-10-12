namespace web.Repositories.Users.Dtos;

public class UpdateUserProfileDto
{
    public string DisplayName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }

    /// <summary>New user name, or null/blank to keep the current one.</summary>
    public string? NewUserName { get; set; }

    /// <summary>New password, or null/empty to keep the current one.</summary>
    public string? NewPassword { get; set; }
}
