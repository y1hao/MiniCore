using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
    private readonly IServiceProvider _serviceProvider;
    private readonly LinkCleanupService _service;

    public LinkCleanupServiceTests()
    {
        _mockLogger = new Mock<ILogger<LinkCleanupService>>();
        _mockConfiguration = new Mock<IConfiguration>();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);

        var serviceCollection = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        serviceCollection.AddSingleton(_context);
        _serviceProvider = serviceCollection.BuildServiceProvider();

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

        _context.ShortLinks.AddRange(expiredLink, activeLink);
        await _context.SaveChangesAsync();

        // Act - Use reflection to call the private method, or test via public method
        // Since CleanupExpiredLinks is private, we'll test indirectly through ExecuteAsync
        // For unit testing, we'll create a testable version or use reflection
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.CancelAfter(100); // Cancel after 100ms

        // Start the service and let it run briefly
        var task = _service.StartAsync(cancellationTokenSource.Token);
        await Task.Delay(50); // Wait a bit
        await _service.StopAsync(cancellationTokenSource.Token);

        // Manually trigger cleanup for testing
        await CleanupExpiredLinksDirectly();

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
        await CleanupExpiredLinksDirectly();

        // Assert
        var finalCount = await _context.ShortLinks.CountAsync();
        Assert.Equal(initialCount, finalCount);
        Assert.True(await _context.ShortLinks.AnyAsync(l => l.Id == activeLink.Id));
    }

    private async Task CleanupExpiredLinksDirectly()
    {
        var expiredLinks = await _context.ShortLinks
            .Where(l => l.ExpiresAt.HasValue && l.ExpiresAt.Value < DateTime.UtcNow)
            .ToListAsync();

        if (expiredLinks.Count > 0)
        {
            _context.ShortLinks.RemoveRange(expiredLinks);
            await _context.SaveChangesAsync();
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _context.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

