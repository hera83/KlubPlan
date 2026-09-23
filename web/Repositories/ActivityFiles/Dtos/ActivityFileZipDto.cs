namespace web.Repositories.ActivityFiles.Dtos
{
    /// <summary>Files to put in a zip download (current version of each file), with their path inside the zip.</summary>
    public class ActivityFileZipDto
    {
        public string ZipFileName { get; set; } = "filer.zip";
        public List<ActivityFileZipEntryDto> Entries { get; set; } = new();

        /// <summary>Folders to create as (possibly empty) directory entries in the zip, e.g. "Planlægning/Budget/".</summary>
        public List<string> EmptyFolders { get; set; } = new();
    }

    public class ActivityFileZipEntryDto
    {
        public string EntryPath { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
    }
}
