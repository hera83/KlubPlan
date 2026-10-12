using Microsoft.EntityFrameworkCore;
using web.Data;
using web.Repositories.ThemeSettings.Dtos;
using web.Repositories.ThemeSettings.Interfaces;
using web.Repositories.ThemeSettings.Mappers;

namespace web.Repositories.ThemeSettings;

public class ThemeSettingRepository(WebDbContext dbContext) : IThemeSettingRepository
{
    public Task<List<ThemeSettingDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        dbContext.ThemeSettings.AsNoTracking()
            .OrderBy(t => t.ThemeMode)
            .Select(ThemeSettingMapper.ToDtoExpression)
            .ToListAsync(cancellationToken);

    public Task<ThemeSettingDto?> GetAsync(string name, string themeMode, CancellationToken cancellationToken = default) =>
        dbContext.ThemeSettings.AsNoTracking()
            .Where(t => t.Name == name && t.ThemeMode == themeMode)
            .Select(ThemeSettingMapper.ToDtoExpression)
            .FirstOrDefaultAsync(cancellationToken);
}
