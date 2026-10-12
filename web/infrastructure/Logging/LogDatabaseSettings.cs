namespace web.Infrastructure.Logging;

/// <summary>
/// Where Serilog's SQLite sink writes (app_dbs/log.db) and how big the file may grow before
/// LogCleanupWorker trims it. Registered as a singleton in Program.cs.
/// </summary>
public class LogDatabaseSettings
{
    public string DatabasePath { get; init; } = string.Empty;

    public string TableName { get; init; } = "Logs";

    public long MaxSizeBytes { get; init; } = 1L * 1024 * 1024 * 1024;
}
