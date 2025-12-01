using MiniCore.Framework.Configuration.Abstractions;
using MiniCore.Framework.Data;
using MiniCore.Framework.Data.Extensions;
using MiniCore.Framework.DependencyInjection;
using MiniCore.Framework.Logging;
using MiniCore.Web.Data;
using MiniCore.Web.Models;
using MiniCore.Web.Services;
using Moq;

namespace MiniCore.Web.Tests.Services;

public class LinkCleanupServiceTests : IDisposable
{
    private readonly Mock<ILogger<LinkCleanupService>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly AppDbContext _context;
    private readonly MiniCore.Framework.DependencyInjection.IServiceProvider _serviceProvider;
    private readonly LinkCleanupService _service;
    private readonly string _tempDbPath;

    public LinkCleanupServiceTests()
    {
        _mockLogger = new Mock<ILogger<LinkCleanupService>>();
        _mockConfiguration = new Mock<IConfiguration>();

        // Use a file-based database for testing to ensure all contexts share the same database
        var tempDbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlite($"Data Source={tempDbPath}");
        var options = optionsBuilder.Options;

        _context = new AppDbContext(options);
        _context.EnsureCreated();

        var serviceCollection = new ServiceCollection();
        // Register DbContextOptions so all contexts use the same database file
        serviceCollection.AddSingleton(options);
        // Register AppDbContext as scoped - return the same instance for testing
        // This ensures the service uses the exact same context instance as the test
        serviceCollection.AddScoped<AppDbContext>(_ => _context);
        // Build provider first, then register IServiceScopeFactory using the built provider
        var tempProvider = serviceCollection.BuildServiceProvider();
        // Register IServiceScopeFactory (ServiceProvider implements it, but we need to register it explicitly)
        serviceCollection.AddSingleton<IServiceScopeFactory>(_ => (IServiceScopeFactory)tempProvider);
        _serviceProvider = serviceCollection.BuildServiceProvider();
        
        // Store the temp path for cleanup
        _tempDbPath = tempDbPath;

        var configurationSection = new Mock<IConfigurationSection>();
        configurationSection.Setup(s => s.Value).Returns("1");
        _mockConfiguration.Setup(c => c.GetSection("LinkCleanup:IntervalHours"))
            .Returns(configurationSection.Object);
        _mockConfiguration.Setup(c => c["LinkCleanup:IntervalHours"]).Returns("1");

        _service = new LinkCleanupService(_serviceProvider, _mockLogger.Object, _mockConfiguration.Object);
    }

    [Fact]
    public async Task CleanupExpiredLinks_RemovesExpiredLinks()
    {
        // Arrange
        var expiredLink = new ShortLink
        {
            ShortCode = "expired1",
            OriginalUrl = "https://example.com",
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            ExpiresAt = DateTime.UtcNow.AddDays(-1) // Expired
        };

        var activeLink = new ShortLink
        {
            ShortCode = "active1",
            OriginalUrl = "https://example.com",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(1) // Not expired
        };

        _context.ShortLinks.Add(expiredLink);
        _context.ShortLinks.Add(activeLink);
        await _context.SaveChangesAsync();

        // Act
        var cancellationTokenSource = new CancellationTokenSource();
        await _service.CleanupExpiredLinks(cancellationTokenSource.Token);

        // Assert
        Assert.False(await _context.ShortLinks.AnyAsync(l => l.Id == expiredLink.Id));
        Assert.True(await _context.ShortLinks.AnyAsync(l => l.Id == activeLink.Id));
    }

    [Fact]
    public async Task CleanupExpiredLinks_DoesNotRemoveActiveLinks()
    {
        // Arrange
        var activeLink = new ShortLink
        {
            ShortCode = "active1",
            OriginalUrl = "https://example.com",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };

        _context.ShortLinks.Add(activeLink);
        await _context.SaveChangesAsync();

        var initialCount = await _context.ShortLinks.CountAsync();

        // Act
        var cancellationTokenSource = new CancellationTokenSource();
        await _service.CleanupExpiredLinks(cancellationTokenSource.Token);

        // Assert - Reload data from database to ensure we see the latest state
        await _context.SaveChangesAsync(); // Ensure any pending changes are saved
        var finalCount = await _context.ShortLinks.CountAsync();
        var activeExists = await _context.ShortLinks.AnyAsync(l => l.Id == activeLink.Id);
        Assert.Equal(initialCount, finalCount);
        Assert.True(activeExists, "Active link should still exist");
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _context.Dispose();
            // Clean up temporary database file
            if (!string.IsNullOrEmpty(_tempDbPath) && File.Exists(_tempDbPath))
            {
                try { File.Delete(_tempDbPath); } catch { }
            }
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

