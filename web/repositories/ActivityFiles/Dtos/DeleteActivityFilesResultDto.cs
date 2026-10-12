namespace web.Repositories.ActivityFiles.Dtos;

/// <summary>What was deleted, and the stored files behind the deleted versions (to delete next).</summary>
public class DeleteActivityFilesResultDto
{
    public DeleteActivityFilesSummaryDto Summary { get; set; } = new();
    public List<int> StoredFileIds { get; set; } = [];
}
