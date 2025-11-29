using MiniCore.Framework.Data;
using MiniCore.Framework.Http;
using MiniCore.Framework.Logging;
using MiniCore.Framework.Mvc.Abstractions;
using MiniCore.Framework.Mvc.Results;
using MiniCore.Web.Controllers;
using MiniCore.Web.Data;
using MiniCore.Web.Models;
using Moq;

namespace MiniCore.Web.Tests.Controllers;

public class RedirectControllerTests : IDisposable
{
    private readonly Mock<ILogger<RedirectController>> _mockLogger;
    private readonly AppDbContext _context;
    private readonly RedirectController _controller;

    public RedirectControllerTests()
    {
        _mockLogger = new Mock<ILogger<RedirectController>>();

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlite(":memory:");
        var options = optionsBuilder.Options;

        _context = new AppDbContext(options);
        _context.EnsureCreated();
        _controller = new RedirectController(_context, _mockLogger.Object);

        // Setup mock HTTP context
        var httpContext = new HttpContext();
        _controller.HttpContext = httpContext;
    }

    [Fact]
    public async Task RedirectToUrl_WithValidShortCode_ReturnsRedirectResult()
    {
        // Arrange
        var link = new ShortLink 
        { 
            ShortCode = "abc123", 
            OriginalUrl = "https://example.com", 
            CreatedAt = DateTime.UtcNow 
        };
        _context.ShortLinks.Add(link);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.RedirectToUrl("abc123");

        // Assert
        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Equal("https://example.com", redirectResult.Url);
    }

    [Fact]
    public async Task RedirectToUrl_WithPathContainingSlash_ExtractsShortCodeCorrectly()
    {
        // Arrange
        var link = new ShortLink 
        { 
            ShortCode = "abc123", 
            OriginalUrl = "https://example.com", 
            CreatedAt = DateTime.UtcNow 
        };
        _context.ShortLinks.Add(link);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.RedirectToUrl("/abc123");

        // Assert
        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Equal("https://example.com", redirectResult.Url);
    }

    [Fact]
    public async Task RedirectToUrl_WithPathContainingMultipleSlashes_PreservesAdditionalPathSegments()
    {
        // Arrange
        var link = new ShortLink 
        { 
            ShortCode = "abc123", 
            OriginalUrl = "https://example.com", 
            CreatedAt = DateTime.UtcNow 
        };
        _context.ShortLinks.Add(link);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.RedirectToUrl("/abc123/some/path");

        // Assert
        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Equal("https://example.com/some/path", redirectResult.Url);
    }

    [Fact]
    public async Task RedirectToUrl_WithNonExistentShortCode_ReturnsNotFound()
    {
        // Act
        var result = await _controller.RedirectToUrl("nonexistent");

        // Assert
        Assert.IsType<NotFoundResult>(result);
        
        // Verify logging
        _mockLogger.Verify(
            x => x.Log(
                MiniCore.Framework.Logging.LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Short code not found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task RedirectToUrl_WithExpiredLink_ReturnsNotFound()
    {
        // Arrange
        var link = new ShortLink 
        { 
            ShortCode = "expired123", 
            OriginalUrl = "https://example.com", 
            CreatedAt = DateTime.UtcNow.AddDays(-60),
            ExpiresAt = DateTime.UtcNow.AddDays(-30) // Expired 30 days ago
        };
        _context.ShortLinks.Add(link);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.RedirectToUrl("expired123");

        // Assert
        Assert.IsType<NotFoundResult>(result);
        
        // Verify logging
        _mockLogger.Verify(
            x => x.Log(
                MiniCore.Framework.Logging.LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Short code expired")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task RedirectToUrl_WithValidLink_LogsRedirectInformation()
    {
        // Arrange
        var link = new ShortLink 
        { 
            ShortCode = "test123", 
            OriginalUrl = "https://example.com", 
            CreatedAt = DateTime.UtcNow 
        };
        _context.ShortLinks.Add(link);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.RedirectToUrl("test123");

        // Assert
        Assert.IsType<RedirectResult>(result);
        
        // Verify logging
        _mockLogger.Verify(
            x => x.Log(
                MiniCore.Framework.Logging.LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Redirecting")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task RedirectToUrl_WithNullPath_ReturnsNotFound()
    {
        // Act
        var result = await _controller.RedirectToUrl(null!);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task RedirectToUrl_WithEmptyPath_ReturnsNotFound()
    {
        // Act
        var result = await _controller.RedirectToUrl("");

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task RedirectToUrl_WithWhitespacePath_ReturnsNotFound()
    {
        // Act
        var result = await _controller.RedirectToUrl("   ");

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task RedirectToUrl_WithLinkNotExpired_ReturnsRedirect()
    {
        // Arrange
        var link = new ShortLink 
        { 
            ShortCode = "future123", 
            OriginalUrl = "https://example.com", 
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(30) // Expires in 30 days
        };
        _context.ShortLinks.Add(link);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.RedirectToUrl("future123");

        // Assert
        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Equal("https://example.com", redirectResult.Url);
    }

    [Fact]
    public async Task RedirectToUrl_WithLinkNoExpiration_ReturnsRedirect()
    {
        // Arrange
        var link = new ShortLink 
        { 
            ShortCode = "noexpiry123", 
            OriginalUrl = "https://example.com", 
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = null // No expiration
        };
        _context.ShortLinks.Add(link);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.RedirectToUrl("noexpiry123");

        // Assert
        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Equal("https://example.com", redirectResult.Url);
    }

    [Fact]
    public async Task RedirectToUrl_WithCaseSensitiveShortCode_ReturnsNotFound_WhenCaseDoesNotMatch()
    {
        // Arrange
        var link = new ShortLink 
        { 
            ShortCode = "ABC123", 
            OriginalUrl = "https://example.com", 
            CreatedAt = DateTime.UtcNow 
        };
        _context.ShortLinks.Add(link);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.RedirectToUrl("abc123"); // Different case

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task RedirectToUrl_WithOriginalUrlEndingWithSlash_PreservesAdditionalPathSegments()
    {
        // Arrange
        var link = new ShortLink 
        { 
            ShortCode = "test123", 
            OriginalUrl = "https://example.com/", 
            CreatedAt = DateTime.UtcNow 
        };
        _context.ShortLinks.Add(link);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.RedirectToUrl("/test123/api/users");

        // Assert
        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Equal("https://example.com/api/users", redirectResult.Url);
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

