using web.Repositories.Users.Dtos;

namespace web.Repositories.Users.Interfaces;

public interface IUserRepository
{
    Task<UserDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Active users who have at least one of the roles, sorted by display name.</summary>
    /// <summary>Does any user exist? (No users = first-user setup.)</summary>
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);

    /// <summary>The user administration table: users by name with roles, filtered on name/e-mail, role and active. Page size 5-200, otherwise 10.</summary>
    Task<UserPageDto> SearchAsync(SearchUsersDto search, CancellationToken cancellationToken = default);

    Task<List<UserOptionDto>> GetActiveInRolesAsync(IReadOnlyCollection<string> roleNames, CancellationToken cancellationToken = default);

    /// <summary>Updates display name and phone, and optionally user name and password (in that order; stops at the first failure).</summary>
    Task<UpdateUserResultDto> UpdateProfileAsync(string id, UpdateUserProfileDto dto, CancellationToken cancellationToken = default);

    /// <summary>Saves the user's theme preference. False if the user doesn't exist.</summary>
    Task<bool> SetThemePreferenceAsync(string id, string themePreference, CancellationToken cancellationToken = default);

    /// <summary>Points the user's profile picture at a stored file (or null for none). False if the user doesn't exist.</summary>
    Task<bool> SetProfilePictureAsync(string id, int? fileId, CancellationToken cancellationToken = default);
}
