namespace web.Repositories.ActivityFiles.Dtos;

/// <summary>One row of the folder tree (the root "Filer" first, then depth-first by name).</summary>
public class ActivityFolderTreeItemDto
{
    public int? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Depth { get; set; }
}
