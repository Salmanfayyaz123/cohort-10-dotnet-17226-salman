using Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public static class DatabaseExtension
{

        // Registers AppDBContext with SQL Server (called in Program.cs)
public static WebApplicationBuilder  AddDatabase(this WebApplicationBuilder builder)
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }
    builder.Services.AddDbContext<AppDBContext>(options =>
    {
        options.UseSqlServer(connectionString);
    });
    return builder;
}
    // Applies pending migrations on startup (called in Program.cs)
    public static async Task MigrateDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDBContext>();
        await dbContext.Database.MigrateAsync();
    }
// Registers Identity (users + roles) using AppDBContext
    public static WebApplicationBuilder AddIdentityServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddIdentityCore<AppUser>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequiredLength = 8;
        })
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<AppDBContext>();

        return builder;
    }

}
