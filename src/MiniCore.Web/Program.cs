using Microsoft.EntityFrameworkCore;
using MiniCore.Web.Data;
using MiniCore.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// TODO: REMOVE IN PHASE 4 (Host Abstraction)
// This bridges ASP.NET Core's Microsoft DI with our custom DI container.
// In Phase 4, we'll replace WebApplication.CreateBuilder() with our own MiniHostBuilder
// that uses our DI natively, eliminating the need for this bridge.
// See: docs/Chapter1/MICROSOFT_DI_DEPENDENCY_ANALYSIS.md
builder.Host.UseServiceProviderFactory(new MiniCore.Web.ServiceProviderFactory());

// TODO: REMOVE IN PHASE 4 (Host Abstraction)
// Build our custom configuration and register it in DI.
// This allows controllers and services to use our custom IConfiguration implementation.
// In Phase 4, we'll build configuration as part of our own HostBuilder.
var customConfiguration = MiniCore.Web.ConfigurationFactory.CreateConfiguration(
    builder.Environment.ContentRootPath,
    builder.Environment.EnvironmentName);

// Register our custom configuration as IConfiguration in DI
// This allows it to be injected into controllers and services
// We use an adapter to bridge our custom configuration with Microsoft's IConfiguration interface
builder.Services.AddSingleton<Microsoft.Extensions.Configuration.IConfiguration>(
    new MiniCore.Web.ConfigurationAdapter(customConfiguration));

// TODO: REMOVE IN PHASE 4 (Host Abstraction)
// Build our custom logging and register it in DI.
// This allows controllers and services to use our custom ILogger implementation.
// In Phase 4, we'll build logging as part of our own HostBuilder.
var loggingFactory = new MiniCore.Framework.Logging.LoggerFactory();
var minLogLevel = builder.Environment.IsDevelopment() 
    ? MiniCore.Framework.Logging.LogLevel.Debug 
    : MiniCore.Framework.Logging.LogLevel.Information;
loggingFactory.AddProvider(new MiniCore.Framework.Logging.Console.ConsoleLoggerProvider(minLogLevel));

// Add file logger if log path is configured
var logPath = customConfiguration["Logging:File:Path"];
if (!string.IsNullOrEmpty(logPath))
{
    var fileMinLevel = Enum.TryParse<MiniCore.Framework.Logging.LogLevel>(customConfiguration["Logging:File:MinLevel"], out var level) 
        ? level 
        : MiniCore.Framework.Logging.LogLevel.Warning;
    loggingFactory.AddProvider(new MiniCore.Framework.Logging.File.FileLoggerProvider(logPath, fileMinLevel));
}

// Register our custom logging factory
// ASP.NET Core will use this factory to create loggers via the adapter
builder.Services.AddSingleton<Microsoft.Extensions.Logging.ILoggerFactory>(
    new MiniCore.Web.LoggingFactoryAdapter(loggingFactory));

// Add services
// Only register SQLite if not in testing environment (tests will register InMemory)
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Add Razor Pages support for views
builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews();

// Register background service (skip in test environment to avoid issues)
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddHostedService<LinkCleanupService>();
}

var app = builder.Build();

// Ensure database is created (skip in test environment)
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.EnsureCreated();
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseStaticFiles();
app.UseRouting();

// Map API controllers first (attribute routing takes precedence)
app.MapControllers();

// Map Razor Pages  
app.MapRazorPages();

// Map redirect endpoint as fallback - only matches if no other route matched
// MapFallbackToController needs a pattern to capture the route parameter
app.MapFallbackToController(
    action: "RedirectToUrl",
    controller: "Redirect",
    pattern: "{*path}");

app.Run();
