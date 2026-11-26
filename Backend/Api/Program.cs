
using Api.Data;
using Api.Extensions;
using Serilog;

// Temporary logger so errors during startup are also logged
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Task Manager API");

var builder = WebApplication.CreateBuilder(args);

  // Replace the built-in logger with Serilog (reads the "Serilog" section in appsettings.json)
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());
// Add services to the container.
builder.AddDatabase();
// Add Identity services (users + roles)

builder.AddIdentityServices();
builder.AddJwtAuthentication();
builder.Services.AddControllers();

var app = builder.Build();

// Local dev runs on http; force https only in production
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.MapControllers();

// Migrate the database on application startup
await app.MigrateDatabaseAsync();
// Create roles + first Admin account
await app.SeedDatabaseAsync();

app.UseAuthentication();   // who are you? (reads the token)
app.UseAuthorization();    // are you allowed? (checks roles)

await app.RunAsync();

}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}