using web.Repositories.ThemeSettings.Dtos;

namespace web.Repositories.ThemeSettings.Interfaces;

/// <summary>The color themes (one row per theme name and mode, seeded by ThemeSettingSeeder).</summary>
public interface IThemeSettingRepository
{
    /// <summary>All themes, ordered by mode.</summary>
    Task<List<ThemeSettingDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>The theme with the name in the mode (ThemeMode.Light/Dark), or null.</summary>
    Task<ThemeSettingDto?> GetAsync(string name, string themeMode, CancellationToken cancellationToken = default);
}
