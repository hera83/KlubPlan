namespace web.Data.Models;

/// <summary>
/// Metadata for files stored in app_files/ directory
/// IMPORTANT: Physical files are NEVER stored in database, only metadata
/// </summary>
public class FileMetadata : EntityBase
{
    /// <summary>
    /// Original filename as uploaded by user
    /// </summary>
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>
    /// Stored filename (typically a GUID or safe unique name)
    /// </summary>
    public string StoredFileName { get; set; } = string.Empty;

    /// <summary>
    /// Path including the root folder, e.g. "app_files/meetings/…" (older rows: "App_files\meetings\…"); resolve with AppFilesHelper.GetFullPath
    /// </summary>
    public string StoredPath { get; set; } = string.Empty;

    /// <summary>
    /// MIME content type (e.g., "image/png", "application/pdf")
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Optional: User ID of the file owner
    /// Can be null if file is system-owned or anonymous
    /// </summary>
    public string? OwnerId { get; set; }

    /// <summary>
    /// Optional: Navigation property to owner
    /// Uncomment in WebDbContext if needed
    /// </summary>
    // public virtual ApplicationUser? Owner { get; set; }

    /// <summary>
    /// Optional: File category corresponding to subfolder under app_files/
    /// Use constants from FileCategories (e.g., "avatars", "uploads", "exports", "temp")
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Optional: Description or notes about the file
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Optional: Indicates if file has been soft-deleted
    /// Physical file should be deleted separately if this is true
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// Optional: UTC timestamp when file was soft-deleted
    /// </summary>
    public DateTime? DeletedAtUtc { get; set; }
}
