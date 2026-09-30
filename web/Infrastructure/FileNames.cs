namespace web.Infrastructure
{
    public static class FileNames
    {
        /// <summary>A download file name (without extension) with characters Windows/browsers reject replaced by '_'.</summary>
        public static string Sanitize(string name, string fallback)
        {
            var invalid = Path.GetInvalidFileNameChars().Concat(new[] { '\\', '/', ':', '*', '?', '"', '<', '>', '|' }).ToHashSet();
            var cleaned = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray()).Trim();
            return cleaned.Length == 0 ? fallback : cleaned;
        }
    }
}
