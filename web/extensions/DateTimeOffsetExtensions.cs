namespace web.Extensions;

/// <summary>Danish display formats for <see cref="DateTimeOffset"/> — same formats as <see cref="DateTimeExtensions"/>.</summary>
public static class DateTimeOffsetExtensions
{
    /// <summary>Formats as "22. sep. 2026 14:30".</summary>
    public static string ToDanishDateTime(this DateTimeOffset value) =>
        value.ToString(DateTimeExtensions.DateTimeFormat, DateTimeExtensions.DanishCulture);

    public static string ToDanishDateTime(this DateTimeOffset? value, string fallback = "–") =>
        value.HasValue ? value.Value.ToDanishDateTime() : fallback;

    /// <summary>Formats as "22. sep. 2026 kl. 14:30".</summary>
    public static string ToDanishDateTimeWithKl(this DateTimeOffset value) =>
        value.ToString(DateTimeExtensions.DateTimeWithKlFormat, DateTimeExtensions.DanishCulture);

    public static string ToDanishDateTimeWithKl(this DateTimeOffset? value, string fallback = "–") =>
        value.HasValue ? value.Value.ToDanishDateTimeWithKl() : fallback;
}
