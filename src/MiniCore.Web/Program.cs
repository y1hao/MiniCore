using MiniCore.Framework.Configuration;
using MiniCore.Framework.Configuration.Abstractions;
using MiniCore.Framework.Data.Extensions;
using MiniCore.Framework.DependencyInjection;
using MiniCore.Framework.Hosting;
using MiniCore.Framework.Logging;
using MiniCore.Framework.Logging.Console;
using MiniCore.Framework.Logging.File;
using MiniCore.Framework.Mvc.Views;
using MiniHostedService = MiniCore.Framework.Hosting.IHostedService;
using MiniLogLevel = MiniCore.Framework.Logging.LogLevel;
using MiniWebApplicationBuilder = MiniCore.Framework.Hosting.WebApplicationBuilder;
using MiniCore.Web.Data;
using MiniCore.Web.Services;

var builder = MiniWebApplicationBuilder.CreateBuilder(args);

// Configure file logging if log path is configured
var logPath = builder.Configuration["Logging:File:Path"];
if (!string.IsNullOrEmpty(logPath))
{
    var fileMinLevel = Enum.TryParse<MiniLogLevel>(builder.Configuration["Logging:File:MinLevel"], out var level) 
        ? level 
        : MiniLogLevel.Warning;
    builder.Host.ConfigureLogging(logging =>
    {
        logging.AddFile(logPath, fileMinLevel);
    });
}

// Add services
// Register ViewEngine for templating
builder.Services.AddSingleton<IViewEngine, ViewEngine>();

// Only register SQLite if not in testing environment (tests will register InMemory)
if (!builder.Environment.IsEnvironment("Testing"))
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrEmpty(connectionString))
    {
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(connectionString));
    }
}

// Register background service (skip in test environment to avoid issues)
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddSingleton(typeof(MiniHostedService), typeof(LinkCleanupService));
}

var app = builder.Build();

// Ensure database is created (skip in test environment)
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.EnsureCreated();
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

// Map redirect endpoint as fallback - only matches if no other route matched
// MapFallbackToController needs a pattern to capture the route parameter
app.MapFallbackToController(
    action: "RedirectToUrl",
    controller: "Redirect",
    pattern: "{*path}");

app.Run();
