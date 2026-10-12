using Microsoft.Data.Sqlite;
using web.Infrastructure.Logging;

namespace web.BgWorkers;

/// <summary>
/// Keeps app_dbs/log.db under <see cref="LogDatabaseSettings.MaxSizeBytes"/>. Every 15 minutes it
/// checks the file size; when it is over the limit it deletes the oldest ~10 % of the rows (at
/// least 1000) and runs VACUUM, repeating until the file is under the limit. A size cap, not a
/// time-based retention policy.
/// </summary>
public class LogCleanupWorker(LogDatabaseSettings settings, ILogger<LogCleanupWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);
    private const int MinRowsPerRound = 1000;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        try
        {
            do
            {
                try
                {
                    await TrimAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Forventet ved nedlukning.
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "{Worker} failed", nameof(LogCleanupWorker));
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // WaitForNextTickAsync kaster, når stoppingToken annulleres.
        }
    }

    private async Task TrimAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(settings.DatabasePath) || new FileInfo(settings.DatabasePath).Length <= settings.MaxSizeBytes)
        {
            return;
        }

        var table = "\"" + settings.TableName.Replace("\"", "\"\"") + "\"";
        await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = settings.DatabasePath }.ToString());
        await connection.OpenAsync(cancellationToken);

        while (new FileInfo(settings.DatabasePath).Length > settings.MaxSizeBytes)
        {
            await using var countCommand = connection.CreateCommand();
            countCommand.CommandText = $"SELECT COUNT(*) FROM {table}";
            var rowCount = Convert.ToInt64(await countCommand.ExecuteScalarAsync(cancellationToken));
            if (rowCount == 0)
            {
                break;
            }

            var rowsToDelete = Math.Max(rowCount / 10, MinRowsPerRound);

            await using var deleteCommand = connection.CreateCommand();
            deleteCommand.CommandText = $"DELETE FROM {table} WHERE id IN (SELECT id FROM {table} ORDER BY id LIMIT @count)";
            deleteCommand.Parameters.AddWithValue("@count", rowsToDelete);
            var deleted = await deleteCommand.ExecuteNonQueryAsync(cancellationToken);

            await using var vacuumCommand = connection.CreateCommand();
            vacuumCommand.CommandText = "VACUUM";
            await vacuumCommand.ExecuteNonQueryAsync(cancellationToken);

            logger.LogWarning("Deleted {Count} old log rows to keep {DatabasePath} under {MaxSizeBytes} bytes",
                deleted, settings.DatabasePath, settings.MaxSizeBytes);
        }
    }
}
