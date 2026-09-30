using web.Repositories.InfoScreens.Dtos;
using web.ViewModels;

namespace web.Repositories.InfoScreens.Interfaces
{
    /// <summary>
    /// Værktøjer → Infoskærme: screens (slideshows) with slides designed in the designer, a media
    /// library per screen (App_files/infoscreens/) and the data for the public, running screen.
    /// </summary>
    public interface IInfoScreenService
    {
        /// <summary>The table page matching the search/filter, sorted by title.</summary>
        Task<InfoScreenFilterViewModel> GetScreensAsync(InfoScreenFilterViewModel filter, CancellationToken ct = default);

        /// <summary>Creates the screen with one starter slide. Result.Id is the new screen's id.</summary>
        Task<InfoScreenActionResultDto> CreateAsync(InfoScreenCreateViewModel input, CancellationToken ct = default);

        Task<InfoScreenActionResultDto> SetActiveAsync(int id, bool isActive, CancellationToken ct = default);

        /// <summary>Deletes the screen, its slides and its media files. False when not found.</summary>
        Task<bool> DeleteAsync(int id, CancellationToken ct = default);

        /// <summary>Null when not found.</summary>
        Task<InfoScreenDesignerViewModel?> GetDesignerAsync(int id, CancellationToken ct = default);

        /// <summary>Validates/normalizes the design and replaces the screen's settings and slides.</summary>
        Task<InfoScreenActionResultDto> SaveDesignAsync(int id, InfoScreenDesignDto design, CancellationToken ct = default);

        /// <summary>Adds an image or video to the screen's media library. Result.Media is the new file.</summary>
        Task<InfoScreenActionResultDto> UploadMediaAsync(int id, Stream content, string fileName, long length, string? ownerId, CancellationToken ct = default);

        /// <summary>Removes a file from the media library — refused while a saved slide uses it.</summary>
        Task<InfoScreenActionResultDto> DeleteMediaAsync(int id, Guid mediaId, CancellationToken ct = default);

        /// <summary>The running screen's data. Preview plays an inactive screen too (logged-in users only).</summary>
        Task<InfoScreenPlayerDto> GetPlayerDataAsync(Guid publicId, bool preview, CancellationToken ct = default);

        /// <summary>Title for the player page's &lt;title&gt;. Null when not found.</summary>
        Task<string?> GetTitleAsync(Guid publicId, CancellationToken ct = default);

        /// <summary>Physical path and content type of a media file. Null when not found.</summary>
        Task<(string FullPath, string ContentType)?> GetMediaFileAsync(Guid mediaId, CancellationToken ct = default);
    }
}
