namespace web.Repositories.ActivityFiles.Dtos;

public class ActivityFolderBreadcrumbDto
{
    /// <summary>Null = the root.</summary>
    public int? Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
