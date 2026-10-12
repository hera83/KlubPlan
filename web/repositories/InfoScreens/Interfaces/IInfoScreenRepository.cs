using web.Repositories.InfoScreens.Dtos;

namespace web.Repositories.InfoScreens.Interfaces;

/// <summary>
/// Værktøjer → Infoskærme: screens (slideshows) with slides and a media library per screen.
/// The media files themselves are FileMetadata rows owned by the Files repository; this repository
/// only stores which file belongs to which screen (InfoScreenMedia).
/// </summary>
public interface IInfoScreenRepository
{
    /// <summary>The table page matching the search/filter, sorted by title.</summary>
    Task<InfoScreenPageDto> SearchAsync(SearchInfoScreensDto search, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Creates the screen with one starter slide. Invalid when the title is blank.</summary>
    Task<SaveInfoScreenResultDto> CreateAsync(CreateInfoScreenDto dto, CancellationToken cancellationToken = default);

    Task<SaveInfoScreenResultDto> SetActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default);

    /// <summary>Deletes the screen with its slides and media rows. Returns the ids of the stored files to delete, or null when not found.</summary>
    Task<List<int>?> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Null when not found.</summary>
    Task<InfoScreenDesignerDto?> GetDesignerAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Validates/normalizes the design (InfoScreenDesignNormalizer) and replaces the screen's settings and slides.</summary>
    Task<SaveInfoScreenResultDto> SaveDesignAsync(int id, InfoScreenDesignDto design, CancellationToken cancellationToken = default);

    /// <summary>Adds a stored file to the screen's media library. Null when the screen doesn't exist.</summary>
    Task<InfoScreenMediaDto?> AddMediaAsync(int id, int fileId, string kind, CancellationToken cancellationToken = default);

    /// <summary>Removes a file from the media library — refused while a saved slide uses it.</summary>
    Task<RemoveInfoScreenMediaResultDto> RemoveMediaAsync(int id, Guid mediaId, CancellationToken cancellationToken = default);

    /// <summary>The stored file behind a media id (InfoScreenMedia.PublicId), or null.</summary>
    Task<int?> GetMediaFileIdAsync(Guid mediaId, CancellationToken cancellationToken = default);

    /// <summary>The running screen's data. Preview plays an inactive screen too.</summary>
    Task<InfoScreenPlayerDto> GetPlayerAsync(Guid publicId, bool preview, CancellationToken cancellationToken = default);

    /// <summary>Title for the player page's &lt;title&gt;. Null when not found.</summary>
    Task<string?> GetTitleAsync(Guid publicId, CancellationToken cancellationToken = default);
}
