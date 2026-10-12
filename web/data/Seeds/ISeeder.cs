namespace web.Data.Seeds;

/// <summary>
/// One seeder per table/purpose. Registered in <see cref="DataSeeder"/> and run at every startup,
/// after migrations, in all environments — so every implementation must be idempotent.
/// </summary>
public interface ISeeder
{
    Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default);
}
