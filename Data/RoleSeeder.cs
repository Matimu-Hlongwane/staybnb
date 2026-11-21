
using Microsoft.AspNetCore.Identity;

namespace Staybnb.Data;

public static class RoleSeeder
{
    public static async Task SeedAsync(
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        using var scope = serviceProvider.CreateScope();

        var roleManager = scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole>>();

        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<IdentityUser>>();

        string[] roles =
        {
            "Guest",
            "Host",
            "Admin",
            "SuperAdmin"
        };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(
                    new IdentityRole(role));

                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Could not create role {role}: " +
                        string.Join(", ",
                            result.Errors.Select(e => e.Description)));
                }
            }
        }

        var email = configuration["SeedAdmin:Email"];
        var password = configuration["SeedAdmin:Password"];

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Configure SeedAdmin:Email and SeedAdmin:Password " +
                "using .NET User Secrets before starting Staybnb.");
        }

        var admin = await userManager.FindByEmailAsync(email);

        if (admin is null)
        {
            admin = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(
                admin, password);

            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Could not create SuperAdmin: " +
                    string.Join(", ",
                        createResult.Errors.Select(e => e.Description)));
            }
        }

        if (!await userManager.IsInRoleAsync(admin, "SuperAdmin"))
        {
            var roleResult = await userManager.AddToRoleAsync(
                admin, "SuperAdmin");

            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Could not assign SuperAdmin role: " +
                    string.Join(", ",
                        roleResult.Errors.Select(e => e.Description)));
            }
        }
    }
}
