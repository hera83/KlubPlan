using Microsoft.EntityFrameworkCore;
using web.Data;
using web.Data.Models;
using web.Repositories.AppSettings.Interfaces;

namespace web.Repositories.AppSettings;

public class AppSettingRepository(WebDbContext dbContext) : IAppSettingRepository
{
    public Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default) =>
        dbContext.AppSettings.AsNoTracking()
            .Where(s => s.Key == key)
            .Select(s => s.Value)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task SetValueAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        // AppSetting isn't an EntityBase (string key), so its timestamps are set here.
        var setting = await dbContext.AppSettings.FirstOrDefaultAsync(s => s.Key == key, cancellationToken);
        if (setting is null)
        {
            dbContext.AppSettings.Add(new AppSetting { Key = key, Value = value, CreatedAtUtc = DateTime.UtcNow });
        }
        else
        {
            setting.Value = value;
            setting.UpdatedAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
