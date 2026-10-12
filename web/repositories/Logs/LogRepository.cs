using Microsoft.Data.Sqlite;
using web.Infrastructure.Logging;
using web.Repositories.Logs.Dtos;
using web.Repositories.Logs.Interfaces;
using web.Repositories.Logs.Mappers;

namespace web.Repositories.Logs;

/// <summary>Read-only, parameterised ADO.NET queries against Serilog's log.db.</summary>
public class LogRepository(LogDatabaseSettings settings, ILogger<LogRepository> logger) : ILogRepository
{
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 500;

    // Serilog's SQLite sink stores Timestamp as UTC text in this (sortable) format.
    private const string TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fff";

    public async Task<LogSearchResultDto> SearchAsync(SearchLogsDto search, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(search.Page, 1);
        var pageSize = search.PageSize is < DefaultPageSize or > MaxPageSize ? DefaultPageSize : search.PageSize;
        var result = new LogSearchResultDto { Page = page, PageSize = pageSize };

        if (!File.Exists(settings.DatabasePath))
        {
            return result;
        }

        var where = new List<string>();
        var parameters = new List<(string Name, object Value)>();

        if (!string.IsNullOrWhiteSpace(search.Level))
        {
            where.Add("Level = @level");
            parameters.Add(("@level", search.Level));
        }

        if (!string.IsNullOrWhiteSpace(search.SearchText))
        {
            where.Add("(RenderedMessage LIKE @search OR Exception LIKE @search OR Properties LIKE @search)");
            parameters.Add(("@search", $"%{search.SearchText}%"));
        }

        if (search.FromUtc.HasValue)
        {
            where.Add("Timestamp >= @fromUtc");
            parameters.Add(("@fromUtc", search.FromUtc.Value.ToString(TimestampFormat)));
        }

        if (search.ToUtc.HasValue)
        {
            where.Add("Timestamp <= @toUtc");
            parameters.Add(("@toUtc", search.ToUtc.Value.ToString(TimestampFormat)));
        }

        var whereSql = where.Count > 0 ? " WHERE " + string.Join(" AND ", where) : string.Empty;
        var table = Quote(settings.TableName);

        try
        {
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = settings.DatabasePath,
                Mode = SqliteOpenMode.ReadOnly
            }.ToString();

            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            await using (var countCommand = connection.CreateCommand())
            {
                countCommand.CommandText = $"SELECT COUNT(*) FROM {table}{whereSql}";
                AddParameters(countCommand, parameters);
                result.TotalCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync(cancellationToken));
            }

            await using var command = connection.CreateCommand();
            command.CommandText = $"SELECT {LogMapper.Columns} FROM {table}{whereSql} ORDER BY id DESC LIMIT @limit OFFSET @offset";
            AddParameters(command, parameters);
            command.Parameters.AddWithValue("@limit", pageSize);
            command.Parameters.AddWithValue("@offset", (page - 1) * pageSize);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                result.Items.Add(reader.ToDto());
            }
        }
        catch (SqliteException ex)
        {
            // The sink creates the table on its first write, so a brand-new log.db may not have it yet.
            logger.LogWarning(ex, "Could not read logs from {DatabasePath}", settings.DatabasePath);
        }

        return result;
    }

    private static void AddParameters(SqliteCommand command, List<(string Name, object Value)> parameters)
    {
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }
    }

    private static string Quote(string identifier) => "\"" + identifier.Replace("\"", "\"\"") + "\"";
}
