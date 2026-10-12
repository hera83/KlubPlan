using web.Repositories.Files.Dtos;

namespace web.Managers.Profile.Interfaces;

public interface IProfileManager
{
    /// <summary>Stores the image as the user's new profile picture and deletes the previous one. False if the user doesn't exist.</summary>
    Task<bool> SetAvatarAsync(string userId, Stream image, string contentType, string originalFileName, CancellationToken cancellationToken = default);

    /// <summary>Removes and deletes the user's profile picture. False if the user doesn't exist or has none.</summary>
    Task<bool> DeleteAvatarAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>The user's profile picture file, or null if there is none.</summary>
    Task<StoredFileDto?> GetAvatarAsync(string userId, CancellationToken cancellationToken = default);
}
