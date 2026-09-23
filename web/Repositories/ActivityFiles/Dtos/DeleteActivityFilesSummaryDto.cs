namespace web.Repositories.ActivityFiles.Dtos
{
    /// <summary>What a delete of a selection would remove — shown in the confirmation modal.</summary>
    public class DeleteActivityFilesSummaryDto
    {
        public int FolderCount { get; set; }
        public int FileCount { get; set; }

        /// <summary>All versions of the files, incl. the current one.</summary>
        public int VersionCount { get; set; }

        public long TotalBytes { get; set; }
    }
}
