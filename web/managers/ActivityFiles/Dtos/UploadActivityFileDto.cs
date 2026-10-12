namespace web.Managers.ActivityFiles.Dtos;

public class UploadActivityFileDto
{
    public int ActivityId { get; set; }

    /// <summary>Folder the upload was started in (null = root).</summary>
    public int? FolderId { get; set; }

    /// <summary>Optional path from a dropped folder, e.g. "Billeder/2026/foto.jpg" — missing folders are created.</summary>
    public string? RelativePath { get; set; }

    /// <summary>When set, the upload becomes a new version of this file regardless of its name.</summary>
    public int? TargetFileId { get; set; }

    public string FileName { get; set; } = string.Empty;
    public long Length { get; set; }
    public string? UserId { get; set; }
}
