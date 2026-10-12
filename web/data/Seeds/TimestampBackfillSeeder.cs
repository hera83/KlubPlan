using Microsoft.EntityFrameworkCore;
using web.Data.Models;

namespace web.Data.Seeds;

/// <summary>
/// Rows that existed before EntityBase had a nullable UpdatedAtUtc (null = never changed). The
/// MakeUpdatedAtRequired migration gives those rows UpdatedAt = 0001-01-01; this sets them to
/// CreatedAt instead, so "UpdatedAt == CreatedAt" keeps meaning "never changed". Idempotent: after
/// the first run no row matches. Can be removed once every environment has started once with it.
/// </summary>
public class TimestampBackfillSeeder : ISeeder
{
    public async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var dbContext = services.GetRequiredService<WebDbContext>();
        var logger = services.GetRequiredService<ILogger<TimestampBackfillSeeder>>();

        var tables = dbContext.Model.GetEntityTypes()
            .Where(t => typeof(EntityBase).IsAssignableFrom(t.ClrType))
            .Select(t => t.GetTableName())
            .OfType<string>()
            .Distinct();

        foreach (var table in tables)
        {
            var sql = "UPDATE \"" + table + "\" SET \"UpdatedAt\" = \"CreatedAt\" WHERE \"UpdatedAt\" < \"CreatedAt\"";
            var updated = await dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
            if (updated > 0)
            {
                logger.LogInformation("Backfilled UpdatedAt on {Count} rows in {Table}", updated, table);
            }
        }
    }
}
