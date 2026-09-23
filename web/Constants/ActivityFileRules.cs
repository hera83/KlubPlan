namespace web.Constants
{
    /// <summary>
    /// Rules for the activity "Filer" tab (document hotel). Program/script files are rejected — the
    /// tab is meant for documents, not as general file storage.
    /// </summary>
    public static class ActivityFileRules
    {
        /// <summary>Max size per uploaded file (50 MB).</summary>
        public const long MaxFileSizeBytes = 50L * 1024 * 1024;

        /// <summary>Request limit for the upload action — a bit above MaxFileSizeBytes to leave room for the multipart envelope.</summary>
        public const long MaxRequestSizeBytes = MaxFileSizeBytes + 1024 * 1024;

        public const int MaxNameLength = 255;

        /// <summary>Executables, installers and scripts that are never accepted (lowercase, incl. dot).</summary>
        public static readonly HashSet<string> BlockedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".exe", ".msi", ".msix", ".msp", ".com", ".scr", ".pif", ".bat", ".cmd", ".ps1", ".psm1", ".psd1",
            ".vbs", ".vbe", ".js", ".jse", ".wsf", ".wsh", ".hta", ".cpl", ".msc", ".jar", ".dll", ".sys",
            ".drv", ".ocx", ".reg", ".lnk", ".inf", ".application", ".appx", ".appxbundle", ".gadget",
            ".sh", ".bash", ".app", ".dmg", ".pkg", ".apk", ".deb", ".rpm", ".iso", ".img", ".vhd", ".vhdx"
        };

        /// <summary>Characters that are not allowed in file and folder names (same set as Windows).</summary>
        public static readonly char[] InvalidNameChars = { '\\', '/', ':', '*', '?', '"', '<', '>', '|' };

        public static bool IsBlocked(string fileName)
            => BlockedExtensions.Contains(Path.GetExtension(fileName));
    }
}
