using Microsoft.AspNetCore.Identity;
using web.Constants;

namespace web.Data.Seeds;

/// <summary>Creates every role in <see cref="AppRoles.AllRoles"/> that doesn't exist yet.</summary>
public class RoleSeeder : ISeeder
{
    public async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var logger = services.GetRequiredService<ILogger<RoleSeeder>>();

        foreach (var roleName in AppRoles.AllRoles)
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            var result = await roleManager.CreateAsync(new IdentityRole(roleName));
            if (result.Succeeded)
            {
                logger.LogInformation("Created role: {RoleName}", roleName);
            }
            else
            {
                logger.LogError("Failed to create role {RoleName}: {Errors}",
                    roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }
}
