using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using web.Data;
using web.Data.Models;
using web.Repositories.Users.Dtos;
using web.Repositories.Users.Interfaces;
using web.Repositories.Users.Mappers;

namespace web.Repositories.Users;

/// <summary>Users (ApplicationUser). Reads go through WebDbContext, writes through Identity's UserManager.</summary>
public class UserRepository(WebDbContext dbContext, UserManager<ApplicationUser> userManager, ILogger<UserRepository> logger) : IUserRepository
{
    public Task<UserDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        dbContext.Users.AsNoTracking()
            .Where(u => u.Id == id)
            .Select(UserMapper.ToDtoExpression)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> AnyAsync(CancellationToken cancellationToken = default) =>
        dbContext.Users.AnyAsync(cancellationToken);

    public async Task<UserPageDto> SearchAsync(SearchUsersDto search, CancellationToken cancellationToken = default)
    {
        var page = search.Page < 1 ? 1 : search.Page;
        var pageSize = search.PageSize is < 5 or > 200 ? 10 : search.PageSize;

        // Few users — roles come from Identity per user, and the filtering is done in memory
        // (case-insensitive for all letters, unlike SQLite).
        var users = await dbContext.Users.AsNoTracking().OrderBy(u => u.DisplayName).ToListAsync(cancellationToken);
        var items = new List<UserListItemDto>();
        foreach (var user in users)
        {
            items.Add(user.ToListItemDto((await userManager.GetRolesAsync(user)).ToList()));
        }

        var query = items.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(search.SearchText))
        {
            var term = search.SearchText.Trim();
            query = query.Where(u =>
                u.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                u.Email.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(search.Role))
        {
            query = query.Where(u => u.Roles.Contains(search.Role));
        }

        if (search.IsActive.HasValue)
        {
            query = query.Where(u => u.IsActive == search.IsActive.Value);
        }

        var filtered = query.ToList();
        return new UserPageDto
        {
            Items = filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            TotalCount = filtered.Count,
            Page = page,
            PageSize = pageSize
        };
    }

    public Task<List<UserOptionDto>> GetActiveInRolesAsync(IReadOnlyCollection<string> roleNames, CancellationToken cancellationToken = default) =>
        dbContext.Users.AsNoTracking()
            .Where(u => u.IsActive && dbContext.UserRoles.Any(ur => ur.UserId == u.Id
                && dbContext.Roles.Any(r => r.Id == ur.RoleId && r.Name != null && roleNames.Contains(r.Name))))
            .OrderBy(u => u.DisplayName)
            .Select(u => new UserOptionDto { Id = u.Id, DisplayName = u.DisplayName })
            .ToListAsync(cancellationToken);

    public async Task<UpdateUserResultDto> UpdateProfileAsync(string id, UpdateUserProfileDto dto, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            return new UpdateUserResultDto { Status = UpdateUserStatus.NotFound };
        }

        user.DisplayName = dto.DisplayName;
        user.PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber;
        user.UpdatedAtUtc = DateTime.UtcNow;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return Rejected(result);
        }

        if (!string.IsNullOrWhiteSpace(dto.NewUserName))
        {
            var existing = await userManager.FindByNameAsync(dto.NewUserName);
            if (existing is not null && existing.Id != user.Id)
            {
                return new UpdateUserResultDto { Status = UpdateUserStatus.UserNameTaken };
            }

            result = await userManager.SetUserNameAsync(user, dto.NewUserName);
            if (!result.Succeeded)
            {
                return Rejected(result);
            }
        }

        if (!string.IsNullOrEmpty(dto.NewPassword))
        {
            result = await userManager.RemovePasswordAsync(user);
            if (!result.Succeeded)
            {
                return Rejected(result);
            }

            result = await userManager.AddPasswordAsync(user, dto.NewPassword);
            if (!result.Succeeded)
            {
                return Rejected(result);
            }
        }

        logger.LogInformation("User {UserId} updated profile", id);
        return new UpdateUserResultDto { Status = UpdateUserStatus.Updated };
    }

    public async Task<bool> SetThemePreferenceAsync(string id, string themePreference, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            return false;
        }

        user.ThemePreference = themePreference;
        user.UpdatedAtUtc = DateTime.UtcNow;
        await userManager.UpdateAsync(user);
        return true;
    }

    public async Task<bool> SetProfilePictureAsync(string id, int? fileId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            return false;
        }

        user.ProfilePictureId = fileId;
        user.UpdatedAtUtc = DateTime.UtcNow;
        await userManager.UpdateAsync(user);
        return true;
    }

    private static UpdateUserResultDto Rejected(IdentityResult result) => new()
    {
        Status = UpdateUserStatus.Rejected,
        ErrorMessage = string.Join(" ", result.Errors.Select(e => e.Description))
    };
}
