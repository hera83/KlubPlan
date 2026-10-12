namespace web.Repositories.ActivityFiles.Dtos;

public class ActivityFileVersionsDto
{
    public int ActivityId { get; set; }
    public int FileId { get; set; }
    public string FileName { get; set; } = string.Empty;

    /// <summary>Newest first; the first one is the current version.</summary>
    public List<ActivityFileVersionItemDto> Versions { get; set; } = [];
}
