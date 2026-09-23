using System.Globalization;

namespace web.Infrastructure
{
    public static class ByteSizeFormatter
    {
        private static readonly string[] Units = { "B", "KB", "MB", "GB", "TB" };
        private static readonly CultureInfo Danish = CultureInfo.GetCultureInfo("da-DK");

        public static string Format(long bytes)
        {
            double size = bytes;
            var unitIndex = 0;
            while (size >= 1024 && unitIndex < Units.Length - 1)
            {
                size /= 1024;
                unitIndex++;
            }

            return $"{size:0.##} {Units[unitIndex]}";
        }

        /// <summary>
        /// Danish display text with one decimal, e.g. "35,2 MB" — matches the client-side
        /// FvFileSize.format in wwwroot/js/activity-files.js.
        /// </summary>
        public static string FormatDanish(long bytes)
        {
            double size = bytes;
            var unitIndex = 0;
            while (size >= 1024 && unitIndex < Units.Length - 1)
            {
                size /= 1024;
                unitIndex++;
            }

            return unitIndex == 0
                ? $"{bytes} {Units[0]}"
                : size.ToString("0.#", Danish) + " " + Units[unitIndex];
        }
    }
}
