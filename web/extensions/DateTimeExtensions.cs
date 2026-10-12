using System.Globalization;

namespace web.Extensions;

/// <summary>
/// Central standard for how dates/datetimes are displayed across the app (tables, details, etc.).
/// Always use these instead of ad-hoc ToString() calls so date formatting stays consistent
/// everywhere and automatically applies to new features. <see cref="DateOnlyExtensions"/> and
/// <see cref="DateTimeOffsetExtensions"/> share the formats defined here.
/// </summary>
public static class DateTimeExtensions
{
    internal static readonly CultureInfo DanishCulture = CultureInfo.GetCultureInfo("da-DK");

    internal const string DateFormat = "d. MMM yyyy";
    internal const string DateTimeFormat = "d. MMM yyyy HH:mm";
    internal const string DateTimeWithKlFormat = "d. MMM yyyy 'kl.' HH:mm";
    internal const string WeekdayDateFormat = "dddd 'den' d. MMM yyyy";
    internal const string WeekdayDateTimeWithKlFormat = "dddd d. MMM yyyy 'kl.' HH:mm";

    /// <summary>Formats as "22. sep. 2026".</summary>
    public static string ToDanishDate(this DateTime value) => value.ToString(DateFormat, DanishCulture);

    public static string ToDanishDate(this DateTime? value, string fallback = "–") =>
        value.HasValue ? value.Value.ToDanishDate() : fallback;

    /// <summary>Formats as "22. sep. 2026 14:30".</summary>
    public static string ToDanishDateTime(this DateTime value) => value.ToString(DateTimeFormat, DanishCulture);

    public static string ToDanishDateTime(this DateTime? value, string fallback = "–") =>
        value.HasValue ? value.Value.ToDanishDateTime() : fallback;

    /// <summary>Formats as "22. sep. 2026 kl. 14:30".</summary>
    public static string ToDanishDateTimeWithKl(this DateTime value) => value.ToString(DateTimeWithKlFormat, DanishCulture);

    public static string ToDanishDateTimeWithKl(this DateTime? value, string fallback = "–") =>
        value.HasValue ? value.Value.ToDanishDateTimeWithKl() : fallback;

    /// <summary>Formats as "tirsdag den 22. sep. 2026".</summary>
    public static string ToDanishWeekdayDate(this DateTime value) => value.ToString(WeekdayDateFormat, DanishCulture);

    /// <summary>Formats as "tirsdag 22. sep. 2026 kl. 14:30".</summary>
    public static string ToDanishWeekdayDateTimeWithKl(this DateTime value) => value.ToString(WeekdayDateTimeWithKlFormat, DanishCulture);

    /// <summary>
    /// A UTC value from the database (SQLite returns DateTime without Kind) as the server's local
    /// time — for &lt;input type="datetime-local"&gt; fields and other local display.
    /// </summary>
    public static DateTime FromUtcToLocal(this DateTime utcValue) => DateTime.SpecifyKind(utcValue, DateTimeKind.Utc).ToLocalTime();

    public static DateTime? FromUtcToLocal(this DateTime? utcValue) => utcValue?.FromUtcToLocal();

    /// <summary>
    /// A value from an &lt;input type="datetime-local"&gt; (the user's local time, no time zone) as UTC
    /// for storing. The server runs in the same time zone as the users.
    /// </summary>
    public static DateTime FromLocalToUtc(this DateTime localValue) => DateTime.SpecifyKind(localValue, DateTimeKind.Local).ToUniversalTime();

    public static DateTime? FromLocalToUtc(this DateTime? localValue) => localValue?.FromLocalToUtc();
}
