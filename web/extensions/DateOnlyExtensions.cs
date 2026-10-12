namespace web.Extensions;

/// <summary>Danish display formats for <see cref="DateOnly"/> — same formats as <see cref="DateTimeExtensions"/>.</summary>
public static class DateOnlyExtensions
{
    /// <summary>Formats as "22. sep. 2026".</summary>
    public static string ToDanishDate(this DateOnly value) =>
        value.ToString(DateTimeExtensions.DateFormat, DateTimeExtensions.DanishCulture);

    public static string ToDanishDate(this DateOnly? value, string fallback = "–") =>
        value.HasValue ? value.Value.ToDanishDate() : fallback;
}
