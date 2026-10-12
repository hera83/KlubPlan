namespace web.Repositories.ActivityFiles.Dtos;

public class ActivityFileItemDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int? FolderId { get; set; }

    /// <summary>"A / B" — only when searching.</summary>
    public string? FolderPath { get; set; }

    public int CurrentVersionId { get; set; }
    public int CurrentVersionNumber { get; set; }
    public int VersionCount { get; set; }
    public long SizeBytes { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public DateTime ModifiedAtUtc { get; set; }
    public string? ModifiedByName { get; set; }
}
