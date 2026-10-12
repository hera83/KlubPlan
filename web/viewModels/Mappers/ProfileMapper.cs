using web.Data.Models;
using web.Repositories.Users.Dtos;

namespace web.ViewModels.Mappers;

/// <summary>Maps between the Profile view models and the Users repository's Dtos.</summary>
public static class ProfileMapper
{
    public static ProfileViewModel ToProfileViewModel(this UserDto user)
    {
        // A user name equal to the email is the default from registration — show it as "not set".
        var userName = user.UserName != user.Email ? user.UserName : string.Empty;

        return new ProfileViewModel
        {
            UserName = userName ?? string.Empty,
            DisplayName = user.DisplayName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            HasAvatar = user.ProfilePictureId.HasValue,
            ThemePreference = user.ThemePreference ?? ThemeMode.System,
            EditForm = new EditProfileViewModel
            {
                UserName = userName,
                DisplayName = user.DisplayName,
                PhoneNumber = user.PhoneNumber
            }
        };
    }

    public static UpdateUserProfileDto ToUpdateUserProfileDto(this EditProfileViewModel model) => new()
    {
        DisplayName = model.DisplayName,
        PhoneNumber = model.PhoneNumber,
        NewUserName = string.IsNullOrWhiteSpace(model.UserName) ? null : model.UserName,
        NewPassword = model.NewPassword
    };
}
