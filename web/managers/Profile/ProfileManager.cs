using web.Constants;
using web.Managers.Profile.Interfaces;
using web.Repositories.Files.Dtos;
using web.Repositories.Files.Interfaces;
using web.Repositories.Users.Interfaces;

namespace web.Managers.Profile;

public class ProfileManager(IUserRepository userRepository, IFileRepository fileRepository, ILogger<ProfileManager> logger) : IProfileManager
{
    public async Task<bool> SetAvatarAsync(string userId, Stream image, string contentType, string originalFileName, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return false;
        }

        var file = await fileRepository.CreateAsync(new CreateStoredFileDto
        {
            // Only the extension matters for the stored name; the cropper always sends PNG or JPEG.
            OriginalFileName = originalFileName,
            ContentType = contentType,
            Category = FileCategories.Avatars,
            OwnerId = userId
        }, image, cancellationToken);

        await userRepository.SetProfilePictureAsync(userId, file.Id, cancellationToken);

        if (user.ProfilePictureId is { } previousId)
        {
            await fileRepository.DeleteAsync(previousId, cancellationToken);
        }

        logger.LogInformation("User {UserId} uploaded avatar {FileId}", userId, file.Id);
        return true;
    }

    public async Task<bool> DeleteAvatarAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user?.ProfilePictureId is not { } fileId)
        {
            return false;
        }

        await userRepository.SetProfilePictureAsync(userId, null, cancellationToken);
        await fileRepository.DeleteAsync(fileId, cancellationToken);

        logger.LogInformation("User {UserId} deleted avatar", userId);
        return true;
    }

    public async Task<StoredFileDto?> GetAvatarAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        return user?.ProfilePictureId is { } fileId
            ? await fileRepository.GetByIdAsync(fileId, cancellationToken)
            : null;
    }
}
