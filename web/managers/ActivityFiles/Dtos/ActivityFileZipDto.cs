namespace web.Managers.ActivityFiles.Dtos;

/// <summary>Files to put in a zip download (current version of each file), with their path inside the zip.</summary>
public class ActivityFileZipDto
{
    public string ZipFileName { get; set; } = "filer.zip";
    public List<ActivityFileZipEntryDto> Entries { get; set; } = [];

    /// <summary>Folders to create as (possibly empty) directory entries in the zip.</summary>
    public List<string> EmptyFolders { get; set; } = [];
}
