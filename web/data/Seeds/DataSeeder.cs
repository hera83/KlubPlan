namespace web.Data.Seeds;

/// <summary>Runs all seeders in order. Called from Program.cs right after MigrateAsync().</summary>
public static class DataSeeder
{
    private static readonly ISeeder[] Seeders =
    [
        new RoleSeeder(),
        new AppSettingSeeder(),
        new ThemeSettingSeeder(),
        new TimestampBackfillSeeder()
    ];

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        foreach (var seeder in Seeders)
        {
            await seeder.SeedAsync(services, cancellationToken);
        }
    }
}
