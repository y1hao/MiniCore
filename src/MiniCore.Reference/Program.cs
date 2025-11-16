using Microsoft.EntityFrameworkCore;
using MiniCore.Reference.Data;
using MiniCore.Reference.Services;

var builder = WebApplication.CreateBuilder(args);

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
