namespace web.Repositories.ActivityFiles.Dtos;

public class ActivityFolderItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Sub folders + files directly in the folder.</summary>
    public int ItemCount { get; set; }

    public DateTime ModifiedAtUtc { get; set; }

    /// <summary>"A / B" — only when searching.</summary>
    public string? ParentPath { get; set; }
}
