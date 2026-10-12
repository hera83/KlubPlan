namespace web.Repositories.ActivityFiles.Dtos;

/// <summary>A version to download: the stored file behind it and the name to save it as.</summary>
public class ActivityFileVersionRefDto
{
    /// <summary>The stored file (Files repository).</summary>
    public int StoredFileId { get; set; }

    public int VersionNumber { get; set; }

    /// <summary>The file's name; older versions get " (v2)" before the extension.</summary>
    public string DownloadName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;
}
