using System.Linq.Expressions;
using web.Data.Models;
using web.Repositories.Users.Dtos;

namespace web.Repositories.Users.Mappers;

public static class UserMapper
{
    public static readonly Expression<Func<ApplicationUser, UserDto>> ToDtoExpression = x => new UserDto
    {
        Id = x.Id,
        UserName = x.UserName,
        Email = x.Email,
        DisplayName = x.DisplayName,
        PhoneNumber = x.PhoneNumber,
        IsActive = x.IsActive,
        ProfilePictureId = x.ProfilePictureId,
        ThemePreference = x.ThemePreference,
        CreatedAtUtc = x.CreatedAtUtc
    };

    private static readonly Func<ApplicationUser, UserDto> CompiledToDto = ToDtoExpression.Compile();

    public static UserDto ToDto(this ApplicationUser entity) => CompiledToDto(entity);

    public static UserListItemDto ToListItemDto(this ApplicationUser entity, List<string> roles) => new()
    {
        Id = entity.Id,
        Email = entity.Email ?? string.Empty,
        DisplayName = entity.DisplayName,
        PhoneNumber = entity.PhoneNumber,
        IsActive = entity.IsActive,
        CreatedAtUtc = entity.CreatedAtUtc,
        Roles = roles
    };
}
