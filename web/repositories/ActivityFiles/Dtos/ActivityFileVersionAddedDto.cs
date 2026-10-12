namespace web.Repositories.ActivityFiles.Dtos;

public class ActivityFileVersionAddedDto
{
    public int FileId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int VersionNumber { get; set; }
}
