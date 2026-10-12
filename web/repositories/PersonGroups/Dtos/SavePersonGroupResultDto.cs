namespace web.Repositories.PersonGroups.Dtos;

/// <summary>Outcome of creating/renaming a group. Name is the trimmed name that was used (or was already taken).</summary>
public class SavePersonGroupResultDto
{
    public SavePersonGroupStatus Status { get; set; }
    public int GroupId { get; set; }
    public string Name { get; set; } = string.Empty;
}
