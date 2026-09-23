namespace web.Repositories.ActivityFiles.Dtos
{
    public class UploadActivityFileResponseDto
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int FileId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public int VersionNumber { get; set; }

        /// <summary>True when an existing file got a new version instead of a new file being created.</summary>
        public bool IsNewVersion => VersionNumber > 1;
    }
}
