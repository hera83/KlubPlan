namespace web.Repositories.ActivityFiles.Dtos;

/// <summary>An older version to restore as the new current version.</summary>
public class RestoreSourceDto
{
    public int FileId { get; set; }
    public string FileName { get; set; } = string.Empty;

    /// <summary>The version's stored file (Files repository).</summary>
    public int StoredFileId { get; set; }

    public bool IsLatest { get; set; }
}
