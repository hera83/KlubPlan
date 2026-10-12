namespace web.Repositories.ActivityFiles.Dtos;

/// <summary>Where an upload goes, after validation (and creating the folders of a dropped folder). ErrorMessage set = refused.</summary>
public class UploadTargetDto
{
    public string? ErrorMessage { get; set; }

    /// <summary>The cleaned file name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The folder the file goes in (null = root).</summary>
    public int? FolderId { get; set; }

    /// <summary>Set when the upload becomes a new version of an existing file.</summary>
    public int? ExistingFileId { get; set; }
}
