using System.Globalization;

namespace web.Infrastructure
{
    /// <summary>
    /// Central standard for how dates/datetimes are displayed across the app (tables, details, etc.).
    /// Always use these instead of ad-hoc ToString() calls so date formatting stays consistent
    /// everywhere and automatically applies to new features.
    /// </summary>
    public static class DateTimeDisplayExtensions
    {
        private static readonly CultureInfo DanishCulture = CultureInfo.GetCultureInfo("da-DK");

        private const string DateFormat = "d. MMM yyyy";
        private const string DateTimeFormat = "d. MMM yyyy HH:mm";
        private const string DateTimeWithKlFormat = "d. MMM yyyy 'kl.' HH:mm";
        private const string WeekdayDateFormat = "dddd 'den' d. MMM yyyy";
        private const string WeekdayDateTimeWithKlFormat = "dddd d. MMM yyyy 'kl.' HH:mm";

        /// <summary>Formats as "22. sep. 2026".</summary>
        public static string ToDanishDate(this DateTime value) => value.ToString(DateFormat, DanishCulture);

        public static string ToDanishDate(this DateTime? value, string fallback = "–") =>
            value.HasValue ? value.Value.ToDanishDate() : fallback;

        public static string ToDanishDate(this DateOnly value) => value.ToString(DateFormat, DanishCulture);

        public static string ToDanishDate(this DateOnly? value, string fallback = "–") =>
            value.HasValue ? value.Value.ToDanishDate() : fallback;

        /// <summary>Formats as "22. sep. 2026 14:30".</summary>
        public static string ToDanishDateTime(this DateTime value) => value.ToString(DateTimeFormat, DanishCulture);

        public static string ToDanishDateTime(this DateTime? value, string fallback = "–") =>
            value.HasValue ? value.Value.ToDanishDateTime() : fallback;

        public static string ToDanishDateTime(this DateTimeOffset value) => value.ToString(DateTimeFormat, DanishCulture);

        public static string ToDanishDateTime(this DateTimeOffset? value, string fallback = "–") =>
            value.HasValue ? value.Value.ToDanishDateTime() : fallback;

        /// <summary>Formats as "22. sep. 2026 kl. 14:30".</summary>
        public static string ToDanishDateTimeWithKl(this DateTime value) => value.ToString(DateTimeWithKlFormat, DanishCulture);

        public static string ToDanishDateTimeWithKl(this DateTime? value, string fallback = "–") =>
            value.HasValue ? value.Value.ToDanishDateTimeWithKl() : fallback;

        public static string ToDanishDateTimeWithKl(this DateTimeOffset value) => value.ToString(DateTimeWithKlFormat, DanishCulture);

        public static string ToDanishDateTimeWithKl(this DateTimeOffset? value, string fallback = "–") =>
            value.HasValue ? value.Value.ToDanishDateTimeWithKl() : fallback;

        /// <summary>Formats as "tirsdag den 22. sep. 2026".</summary>
        public static string ToDanishWeekdayDate(this DateTime value) => value.ToString(WeekdayDateFormat, DanishCulture);

        /// <summary>Formats as "tirsdag 22. sep. 2026 kl. 14:30".</summary>
        public static string ToDanishWeekdayDateTimeWithKl(this DateTime value) => value.ToString(WeekdayDateTimeWithKlFormat, DanishCulture);
    }
}
