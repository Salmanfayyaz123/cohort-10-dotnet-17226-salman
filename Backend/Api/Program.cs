
using System.Security.Claims;
using System.Text.Json.Serialization;
using Api.Data;
using Api.Extensions;
using Api.Middleware;
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

// Global error handling
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
// Accept enums as text in JSON ("Admin") instead of numbers (0)
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

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
// Must be first, so it catches errors from everything after it
app.UseExceptionHandler();

app.UseAuthentication();   // who are you? (reads the token)
app.UseAuthorization();    // are you allowed? (checks roles)

// One log line per request, including WHO made it
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate =
        "HTTP {RequestMethod} {RequestPath} by {UserId} responded {StatusCode} in {Elapsed:0.0} ms";

    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        diagnosticContext.Set("UserId", userId ?? "anonymous");
    };
});

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