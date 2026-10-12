namespace web.Extensions;

/// <summary>
/// Paths under app_files/. New rows store FileMetadata.StoredPath relative to app_files/
/// ("avatars/3f2a….png"). Older rows include the root folder — "app_files/meetings/…", or from
/// before the folder was renamed "App_files/meetings/…", possibly with Windows separators —
/// so <see cref="GetFullPath"/> accepts all of them and the same rows resolve on Windows and Linux.
/// </summary>
public static class AppFilesHelper
{
    public const string FolderName = "app_files";

    private static readonly string[] RootPrefixes = [FolderName + "/", "App_files/"];

    public static string GetFullPath(string contentRootPath, string storedPath) =>
        Path.Combine(contentRootPath, FolderName, ToRelativePath(storedPath));

    /// <summary>The path relative to app_files/ with forward slashes, whichever format it was stored in.</summary>
    public static string ToRelativePath(string storedPath)
    {
        var normalized = storedPath.Replace('\\', '/');
        foreach (var prefix in RootPrefixes)
        {
            if (normalized.StartsWith(prefix, StringComparison.Ordinal))
            {
                return normalized[prefix.Length..];
            }
        }

        return normalized;
    }
}
