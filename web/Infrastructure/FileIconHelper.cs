namespace web.Infrastructure
{
    /// <summary>Maps a file name/content type to a Bootstrap Icons class and tells whether the browser can show it inline.</summary>
    public static class FileIconHelper
    {
        private static readonly Dictionary<string, string> IconsByExtension = new(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "bi-file-earmark-pdf",
            [".doc"] = "bi-file-earmark-word", [".docx"] = "bi-file-earmark-word", [".odt"] = "bi-file-earmark-word", [".rtf"] = "bi-file-earmark-word",
            [".xls"] = "bi-file-earmark-excel", [".xlsx"] = "bi-file-earmark-excel", [".ods"] = "bi-file-earmark-excel",
            [".csv"] = "bi-filetype-csv",
            [".ppt"] = "bi-file-earmark-ppt", [".pptx"] = "bi-file-earmark-ppt", [".odp"] = "bi-file-earmark-ppt",
            [".txt"] = "bi-file-earmark-text", [".md"] = "bi-file-earmark-text",
            [".jpg"] = "bi-file-earmark-image", [".jpeg"] = "bi-file-earmark-image", [".png"] = "bi-file-earmark-image",
            [".gif"] = "bi-file-earmark-image", [".webp"] = "bi-file-earmark-image", [".bmp"] = "bi-file-earmark-image",
            [".heic"] = "bi-file-earmark-image", [".svg"] = "bi-file-earmark-image",
            [".mp3"] = "bi-file-earmark-music", [".wav"] = "bi-file-earmark-music", [".m4a"] = "bi-file-earmark-music", [".ogg"] = "bi-file-earmark-music",
            [".mp4"] = "bi-file-earmark-play", [".mov"] = "bi-file-earmark-play", [".avi"] = "bi-file-earmark-play", [".webm"] = "bi-file-earmark-play",
            [".zip"] = "bi-file-earmark-zip", [".rar"] = "bi-file-earmark-zip", [".7z"] = "bi-file-earmark-zip",
        };

        public static string GetIconClass(string fileName)
            => IconsByExtension.TryGetValue(Path.GetExtension(fileName), out var icon) ? icon : "bi-file-earmark";

        /// <summary>SVG is deliberately excluded — shown inline it could run script in the app's origin.</summary>
        public static bool CanPreview(string contentType)
        {
            if (string.IsNullOrEmpty(contentType))
                return false;

            var ct = contentType.ToLowerInvariant();
            return ct == "application/pdf"
                || ct == "text/plain"
                || (ct.StartsWith("image/") && ct != "image/svg+xml")
                || ct.StartsWith("audio/")
                || ct.StartsWith("video/");
        }
    }
}
