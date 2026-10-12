using System.Globalization;
using Microsoft.Data.Sqlite;
using web.Repositories.Logs.Dtos;

namespace web.Repositories.Logs.Mappers;

public static class LogMapper
{
    /// <summary>The columns <see cref="ToDto"/> reads, in ordinal order.</summary>
    public const string Columns = "id, Timestamp, Level, RenderedMessage, Exception, Properties";

    public static LogEntryDto ToDto(this SqliteDataReader reader) => new()
    {
        Id = reader.IsDBNull(0) ? 0 : reader.GetInt64(0),
        TimestampUtc = ReadUtc(reader, 1),
        Level = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
        Message = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
        Exception = reader.IsDBNull(4) ? null : reader.GetString(4),
        Properties = reader.IsDBNull(5) ? null : reader.GetString(5)
    };

    private static DateTime ReadUtc(SqliteDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
        {
            return DateTime.MinValue;
        }

        return DateTime.TryParse(reader.GetString(ordinal), CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed)
            ? DateTime.SpecifyKind(parsed, DateTimeKind.Utc)
            : DateTime.MinValue;
    }
}
