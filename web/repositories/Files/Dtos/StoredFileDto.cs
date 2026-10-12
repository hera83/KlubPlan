namespace web.Repositories.Files.Dtos;

/// <summary>A file in app_files/ and its metadata. FullPath is absolute, for PhysicalFile/streaming.</summary>
public class StoredFileDto
{
    public int Id { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string? Category { get; set; }
    public string? OwnerId { get; set; }
    public string FullPath { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
