namespace web.Repositories.InfoScreens.Dtos;

/// <summary>Outcome of removing a file from a screen's media library. FileId is the stored file to delete (when Removed).</summary>
public class RemoveInfoScreenMediaResultDto
{
    public RemoveInfoScreenMediaStatus Status { get; set; }
    public int FileId { get; set; }
}
