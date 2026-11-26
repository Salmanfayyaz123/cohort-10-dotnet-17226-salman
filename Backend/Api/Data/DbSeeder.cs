using Api.Enums;
using Api.Models;
using Microsoft.AspNetCore.Identity;

namespace Api.Data;

public static class DbSeeder
{
public static async Task SeedDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;

         var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<AppUser>>();
        var configuration = services.GetRequiredService<IConfiguration>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

        await SeedRolesAsync(roleManager, logger);
        await SeedAdminAsync(userManager, configuration, logger);
    }

 

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager, ILogger logger)
    {
       // Creates one role per value in the UserRole enum: "Admin", "User"
        foreach (var roleName in Enum.GetNames<UserRole>())
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            var result = await roleManager.CreateAsync(new IdentityRole(roleName));

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to create role '{roleName}': {errors}");
            }

            logger.LogInformation("Role {Role} created", roleName);
        }
    }
       private static async Task SeedAdminAsync(UserManager<AppUser> userManager, IConfiguration configuration, ILogger logger)
    {
          var name = configuration["SeedAdmin:Name"];
        var email = configuration["SeedAdmin:Email"];
        var password = configuration["SeedAdmin:Password"];

         if (string.IsNullOrWhiteSpace(name) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("SeedAdmin settings are missing. Skipping admin seeding.");
            return;
        }
         // Already exists? Do nothing.
        var existingAdmin = await userManager.FindByEmailAsync(email);
        if (existingAdmin is not null)
        {
            return;
        }

        var admin = new AppUser
        {
            Name = name,
            Email = email,
            UserName = email,
            EmailConfirmed = true
        };
         var createResult = await userManager.CreateAsync(admin, password);
        if (!createResult.Succeeded)
        {
            var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create admin user: {errors}");
        }

        var roleResult = await userManager.AddToRoleAsync(admin, nameof(UserRole.Admin));
        if (!roleResult.Succeeded)
        {
            var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to assign Admin role: {errors}");
        }

        logger.LogInformation("Admin user {Email} created", email);
    }
}
