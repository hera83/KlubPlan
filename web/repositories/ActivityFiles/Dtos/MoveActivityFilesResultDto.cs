namespace web.Repositories.ActivityFiles.Dtos;

public class MoveActivityFilesResultDto
{
    public int MovedCount { get; set; }

    /// <summary>Items that could not be moved, with the reason, e.g. "Budget.xlsx (navnet findes allerede i destinationen)".</summary>
    public List<string> Skipped { get; set; } = [];
}
