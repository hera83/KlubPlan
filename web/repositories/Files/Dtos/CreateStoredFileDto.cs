namespace web.Repositories.Files.Dtos;

public class CreateStoredFileDto
{
    /// <summary>The name the user/system supplied. Only kept as metadata (download name), never used in the path.</summary>
    public string OriginalFileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    /// <summary>Sub folder of app_files/, see <see cref="web.Constants.FileCategories"/>.</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Optional folder below the category, e.g. the owning record's id.</summary>
    public string? SubFolder { get; set; }

    /// <summary>The ApplicationUser who uploaded the file, if any.</summary>
    public string? OwnerId { get; set; }
}
