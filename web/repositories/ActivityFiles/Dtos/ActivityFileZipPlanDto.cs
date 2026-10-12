namespace web.Repositories.ActivityFiles.Dtos;

/// <summary>What goes into a zip download: entry paths with the stored file behind each, and the (possibly empty) folders.</summary>
public class ActivityFileZipPlanDto
{
    public string ZipFileName { get; set; } = "filer.zip";
    public List<ActivityFileZipPlanEntryDto> Entries { get; set; } = [];

    /// <summary>Folders to create as directory entries, e.g. "Planlægning/Budget/".</summary>
    public List<string> EmptyFolders { get; set; } = [];
}
