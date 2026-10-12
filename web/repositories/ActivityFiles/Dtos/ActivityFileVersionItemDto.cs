namespace web.Repositories.ActivityFiles.Dtos;

public class ActivityFileVersionItemDto
{
    public int Id { get; set; }
    public int VersionNumber { get; set; }
    public long SizeBytes { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string? UploadedByName { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public bool IsCurrent { get; set; }
}
