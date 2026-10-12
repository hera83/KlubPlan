using Microsoft.EntityFrameworkCore;
using web.Data.Models;

namespace web.Data.Seeds;

/// <summary>
/// Seeds the color theme presets (Light/Dark), adding any preset that is missing without touching
/// presets that already exist. First renames legacy preset names, so installs seeded before a
/// rename keep their existing colors under the new name.
/// </summary>
public class ThemeSettingSeeder : ISeeder
{
    private static readonly Dictionary<string, string> LegacyRenames = new()
    {
        ["Default"] = "Orange",
        ["Blåtonet"] = "Blå"
    };

    public async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var dbContext = services.GetRequiredService<WebDbContext>();
        var logger = services.GetRequiredService<ILogger<ThemeSettingSeeder>>();

        await RenameLegacyPresetsAsync(dbContext, logger, cancellationToken);

        var existing = await dbContext.ThemeSettings
            .Select(t => new { t.Name, t.ThemeMode })
            .ToListAsync(cancellationToken);

        var missingPresets = CreatePresets()
            .Where(p => !existing.Any(e => e.Name == p.Name && e.ThemeMode == p.ThemeMode))
            .ToList();

        if (missingPresets.Count > 0)
        {
            dbContext.ThemeSettings.AddRange(missingPresets);
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seeded {Count} missing theme presets", missingPresets.Count);
        }
    }

    private static async Task RenameLegacyPresetsAsync(WebDbContext dbContext, ILogger logger, CancellationToken cancellationToken)
    {
        var changed = false;

        foreach (var (oldName, newName) in LegacyRenames)
        {
            var rows = await dbContext.ThemeSettings.Where(t => t.Name == oldName).ToListAsync(cancellationToken);
            if (rows.Count == 0)
            {
                continue;
            }

            if (await dbContext.ThemeSettings.AnyAsync(t => t.Name == newName, cancellationToken))
            {
                continue;
            }

            foreach (var row in rows)
            {
                row.Name = newName;
            }

            logger.LogInformation("Renamed theme preset {OldName} to {NewName}", oldName, newName);
            changed = true;
        }

        var activeThemeSetting = await dbContext.AppSettings.FirstOrDefaultAsync(s => s.Key == AppSettingKeys.ActiveThemeName, cancellationToken);
        if (activeThemeSetting is not null && LegacyRenames.TryGetValue(activeThemeSetting.Value, out var mappedName))
        {
            activeThemeSetting.Value = mappedName;
            changed = true;
        }

        if (changed)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static List<ThemeSetting> CreatePresets() =>
    [
        // Orange Light Theme
        new ThemeSetting
        {
            Name = "Orange",
            ThemeMode = ThemeMode.Light,
            PrimaryColor = "#c94e24",
            SecondaryColor = "#4b5563",
            SuccessColor = "#16803c",
            DangerColor = "#c8160c",
            WarningColor = "#a16207",
            InfoColor = "#1d5db5",
            BackgroundColor = "#f0f2f5",
            SurfaceColor = "#ffffff",
            SidebarBackgroundColor = "#ffffff",
            TextColor = "#111827",
            TextMutedColor = "#4b5563",
            BorderColor = "#d1d5db"
        },
        // Orange Dark Theme
        new ThemeSetting
        {
            Name = "Orange",
            ThemeMode = ThemeMode.Dark,
            PrimaryColor = "#e85d2e",
            SecondaryColor = "#7d8590",
            SuccessColor = "#2ea043",
            DangerColor = "#f85149",
            WarningColor = "#d29922",
            InfoColor = "#388bfd",
            BackgroundColor = "#0d1117",
            SurfaceColor = "#161b22",
            SidebarBackgroundColor = "#161b22",
            TextColor = "#e6edf3",
            TextMutedColor = "#7d8590",
            BorderColor = "#30363d"
        },
        // Blå Light Theme
        new ThemeSetting
        {
            Name = "Blå",
            ThemeMode = ThemeMode.Light,
            PrimaryColor = "#2563eb",
            SecondaryColor = "#475569",
            SuccessColor = "#16803c",
            DangerColor = "#c8160c",
            WarningColor = "#a16207",
            InfoColor = "#0891b2",
            BackgroundColor = "#eff4fb",
            SurfaceColor = "#ffffff",
            SidebarBackgroundColor = "#ffffff",
            TextColor = "#0f172a",
            TextMutedColor = "#475569",
            BorderColor = "#cbd5e1"
        },
        // Blå Dark Theme
        new ThemeSetting
        {
            Name = "Blå",
            ThemeMode = ThemeMode.Dark,
            PrimaryColor = "#3b82f6",
            SecondaryColor = "#7d8590",
            SuccessColor = "#2ea043",
            DangerColor = "#f85149",
            WarningColor = "#d29922",
            InfoColor = "#22d3ee",
            BackgroundColor = "#0b1220",
            SurfaceColor = "#111a2e",
            SidebarBackgroundColor = "#111a2e",
            TextColor = "#e6edf3",
            TextMutedColor = "#7d8590",
            BorderColor = "#243044"
        },
        // Grøn Light Theme
        new ThemeSetting
        {
            Name = "Grøn",
            ThemeMode = ThemeMode.Light,
            PrimaryColor = "#15803d",
            SecondaryColor = "#4b5563",
            SuccessColor = "#16803c",
            DangerColor = "#c8160c",
            WarningColor = "#a16207",
            InfoColor = "#0e7490",
            BackgroundColor = "#eef7f0",
            SurfaceColor = "#ffffff",
            SidebarBackgroundColor = "#ffffff",
            TextColor = "#0f291a",
            TextMutedColor = "#4b5563",
            BorderColor = "#cfe3d4"
        },
        // Grøn Dark Theme
        new ThemeSetting
        {
            Name = "Grøn",
            ThemeMode = ThemeMode.Dark,
            PrimaryColor = "#22c55e",
            SecondaryColor = "#7d8590",
            SuccessColor = "#2ea043",
            DangerColor = "#f85149",
            WarningColor = "#d29922",
            InfoColor = "#22d3ee",
            BackgroundColor = "#0b1410",
            SurfaceColor = "#11201a",
            SidebarBackgroundColor = "#11201a",
            TextColor = "#e6f3ea",
            TextMutedColor = "#7d8590",
            BorderColor = "#1f3329"
        },
        // Lilla Light Theme
        new ThemeSetting
        {
            Name = "Lilla",
            ThemeMode = ThemeMode.Light,
            PrimaryColor = "#7c3aed",
            SecondaryColor = "#4b5563",
            SuccessColor = "#16803c",
            DangerColor = "#c8160c",
            WarningColor = "#a16207",
            InfoColor = "#0891b2",
            BackgroundColor = "#f4f0fb",
            SurfaceColor = "#ffffff",
            SidebarBackgroundColor = "#ffffff",
            TextColor = "#211238",
            TextMutedColor = "#4b5563",
            BorderColor = "#ddd3f3"
        },
        // Lilla Dark Theme
        new ThemeSetting
        {
            Name = "Lilla",
            ThemeMode = ThemeMode.Dark,
            PrimaryColor = "#a78bfa",
            SecondaryColor = "#7d8590",
            SuccessColor = "#2ea043",
            DangerColor = "#f85149",
            WarningColor = "#d29922",
            InfoColor = "#38bdf8",
            BackgroundColor = "#15101f",
            SurfaceColor = "#1c1730",
            SidebarBackgroundColor = "#1c1730",
            TextColor = "#ece8f9",
            TextMutedColor = "#7d8590",
            BorderColor = "#2e2650"
        },
        // Rød Light Theme
        new ThemeSetting
        {
            Name = "Rød",
            ThemeMode = ThemeMode.Light,
            PrimaryColor = "#dc2626",
            SecondaryColor = "#4b5563",
            SuccessColor = "#16803c",
            DangerColor = "#c8160c",
            WarningColor = "#a16207",
            InfoColor = "#1d5db5",
            BackgroundColor = "#fcecec",
            SurfaceColor = "#ffffff",
            SidebarBackgroundColor = "#ffffff",
            TextColor = "#2a0e0e",
            TextMutedColor = "#4b5563",
            BorderColor = "#f0cccb"
        },
        // Rød Dark Theme
        new ThemeSetting
        {
            Name = "Rød",
            ThemeMode = ThemeMode.Dark,
            PrimaryColor = "#f87171",
            SecondaryColor = "#7d8590",
            SuccessColor = "#2ea043",
            DangerColor = "#f85149",
            WarningColor = "#d29922",
            InfoColor = "#388bfd",
            BackgroundColor = "#1a0e0e",
            SurfaceColor = "#241414",
            SidebarBackgroundColor = "#241414",
            TextColor = "#f8e6e6",
            TextMutedColor = "#7d8590",
            BorderColor = "#3d1f1f"
        },
        // Teal Light Theme
        new ThemeSetting
        {
            Name = "Teal",
            ThemeMode = ThemeMode.Light,
            PrimaryColor = "#0d9488",
            SecondaryColor = "#4b5563",
            SuccessColor = "#16803c",
            DangerColor = "#c8160c",
            WarningColor = "#a16207",
            InfoColor = "#0891b2",
            BackgroundColor = "#eafaf7",
            SurfaceColor = "#ffffff",
            SidebarBackgroundColor = "#ffffff",
            TextColor = "#0d2b27",
            TextMutedColor = "#4b5563",
            BorderColor = "#c9ebe5"
        },
        // Teal Dark Theme
        new ThemeSetting
        {
            Name = "Teal",
            ThemeMode = ThemeMode.Dark,
            PrimaryColor = "#2dd4bf",
            SecondaryColor = "#7d8590",
            SuccessColor = "#2ea043",
            DangerColor = "#f85149",
            WarningColor = "#d29922",
            InfoColor = "#22d3ee",
            BackgroundColor = "#081513",
            SurfaceColor = "#0f201d",
            SidebarBackgroundColor = "#0f201d",
            TextColor = "#e3f7f3",
            TextMutedColor = "#7d8590",
            BorderColor = "#1c3a35"
        },
        // Slate Light Theme
        new ThemeSetting
        {
            Name = "Slate",
            ThemeMode = ThemeMode.Light,
            PrimaryColor = "#475569",
            SecondaryColor = "#64748b",
            SuccessColor = "#16803c",
            DangerColor = "#c8160c",
            WarningColor = "#a16207",
            InfoColor = "#1d5db5",
            BackgroundColor = "#f1f4f7",
            SurfaceColor = "#ffffff",
            SidebarBackgroundColor = "#ffffff",
            TextColor = "#1e293b",
            TextMutedColor = "#64748b",
            BorderColor = "#d8dee5"
        },
        // Slate Dark Theme
        new ThemeSetting
        {
            Name = "Slate",
            ThemeMode = ThemeMode.Dark,
            PrimaryColor = "#94a3b8",
            SecondaryColor = "#7d8590",
            SuccessColor = "#2ea043",
            DangerColor = "#f85149",
            WarningColor = "#d29922",
            InfoColor = "#388bfd",
            BackgroundColor = "#0f1419",
            SurfaceColor = "#161c22",
            SidebarBackgroundColor = "#161c22",
            TextColor = "#e7ebef",
            TextMutedColor = "#7d8590",
            BorderColor = "#2a323c"
        }
    ];
}
