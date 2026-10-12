using Microsoft.EntityFrameworkCore;
using web.Data.Models;

namespace web.Data.Seeds;

/// <summary>Seeds the default application settings when the AppSettings table is empty.</summary>
public class AppSettingSeeder : ISeeder
{
    public async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var dbContext = services.GetRequiredService<WebDbContext>();
        var logger = services.GetRequiredService<ILogger<AppSettingSeeder>>();

        if (await dbContext.AppSettings.AnyAsync(cancellationToken))
        {
            return;
        }

        List<AppSetting> defaultSettings =
        [
            new AppSetting
            {
                Key = AppSettingKeys.AllowPublicRegistration,
                Value = "false",
                Description = "Controls whether public user registration is allowed"
            },
            new AppSetting
            {
                Key = AppSettingKeys.ActiveThemeMode,
                Value = ThemeMode.System,
                Description = "Active theme mode: Light, Dark, or System"
            },
            new AppSetting
            {
                Key = AppSettingKeys.ActiveThemeName,
                Value = "Orange",
                Description = "Name of the active color theme, used for both Light and Dark mode"
            },
            new AppSetting
            {
                Key = AppSettingKeys.SiteName,
                Value = "KlubPlan",
                Description = "Application display name"
            },
            new AppSetting
            {
                Key = AppSettingKeys.SiteLogo,
                Value = "/images/logo.png",
                Description = "Application logo path"
            }
        ];

        dbContext.AppSettings.AddRange(defaultSettings);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded {Count} default settings", defaultSettings.Count);
    }
}
