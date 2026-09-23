namespace web.Repositories.ActivityFiles.Dtos
{
    public class ActivityFileDownloadDto
    {
        public string FullPath { get; set; } = string.Empty;
        public string ContentType { get; set; } = "application/octet-stream";

        /// <summary>Name the browser saves the file as. Older versions get " (v2)" appended before the extension.</summary>
        public string FileName { get; set; } = string.Empty;

        public bool CanPreview { get; set; }
    }
}
