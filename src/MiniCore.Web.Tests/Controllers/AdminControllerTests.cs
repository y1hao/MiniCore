using MiniCore.Framework.Data;
using MiniCore.Framework.Http;
using MiniCore.Framework.Mvc.Abstractions;
using MiniCore.Framework.Mvc.Results;
using MiniCore.Web.Controllers;
using MiniCore.Web.Data;
using MiniCore.Web.Models;

namespace MiniCore.Web.Tests.Controllers;

public class AdminControllerTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly AdminController _controller;

    public AdminControllerTests()
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlite(":memory:");
        var options = optionsBuilder.Options;

        _context = new AppDbContext(options);
        _context.EnsureCreated();
        _controller = new AdminController(_context);

        // Setup mock HTTP context
        var httpContext = new HttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("localhost:5000");
        _controller.HttpContext = httpContext;
    }

    [Fact]
    public async Task Index_ReturnsViewResult_WithLinksOrderedByCreatedAtDescending()
    {
        // Arrange
        var link1 = new ShortLink 
        { 
            ShortCode = "abc123", 
            OriginalUrl = "https://example.com", 
            CreatedAt = DateTime.UtcNow.AddHours(-2) 
        };
        var link2 = new ShortLink 
        { 
            ShortCode = "def456", 
            OriginalUrl = "https://test.com", 
            CreatedAt = DateTime.UtcNow.AddHours(-1) 
        };
        var link3 = new ShortLink 
        { 
            ShortCode = "ghi789", 
            OriginalUrl = "https://sample.com", 
            CreatedAt = DateTime.UtcNow 
        };
        _context.ShortLinks.Add(link1);
        _context.ShortLinks.Add(link2);
        _context.ShortLinks.Add(link3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<object>>(viewResult.Model);
        var linksList = model.ToList();
        Assert.Equal(3, linksList.Count);
        // Verify ordering: most recent first
        var firstLink = linksList[0];
        var secondLink = linksList[1];
        var thirdLink = linksList[2];
        Assert.Equal("ghi789", GetShortCode(firstLink));
        Assert.Equal("def456", GetShortCode(secondLink));
        Assert.Equal("abc123", GetShortCode(thirdLink));
    }

    [Fact]
    public async Task Index_ReturnsViewResult_WithEmptyList_WhenNoLinksExist()
    {
        // Act
        var result = await _controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<object>>(viewResult.Model);
        Assert.Empty(model);
    }

    [Fact]
    public async Task Index_ReturnsViewResult_WithCorrectShortUrl()
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
        var result = await _controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<object>>(viewResult.Model);
        var linkDto = model.First();
        Assert.Equal("https://localhost:5000/test123", GetShortUrl(linkDto));
    }

    [Fact]
    public async Task Index_ReturnsViewResult_WithExpiresAt_WhenSet()
    {
        // Arrange
        var expiresAt = DateTime.UtcNow.AddDays(30);
        var link = new ShortLink 
        { 
            ShortCode = "expiring123", 
            OriginalUrl = "https://example.com", 
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt
        };
        _context.ShortLinks.Add(link);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<object>>(viewResult.Model);
        var linkDto = model.First();
        var expiresAtValue = GetExpiresAt(linkDto);
        Assert.NotNull(expiresAtValue);
        Assert.Equal(expiresAt, expiresAtValue.Value, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Index_ReturnsViewResult_WithNullExpiresAt_WhenNotSet()
    {
        // Arrange
        var link = new ShortLink 
        { 
            ShortCode = "noexpiry123", 
            OriginalUrl = "https://example.com", 
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = null
        };
        _context.ShortLinks.Add(link);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<object>>(viewResult.Model);
        var linkDto = model.First();
        Assert.Null(GetExpiresAt(linkDto));
    }

    private static string GetShortCode(object linkDto)
    {
        var prop = linkDto.GetType().GetProperty("ShortCode");
        return prop?.GetValue(linkDto)?.ToString() ?? string.Empty;
    }

    private static string GetShortUrl(object linkDto)
    {
        var prop = linkDto.GetType().GetProperty("ShortUrl");
        return prop?.GetValue(linkDto)?.ToString() ?? string.Empty;
    }

    private static DateTime? GetExpiresAt(object linkDto)
    {
        var prop = linkDto.GetType().GetProperty("ExpiresAt");
        return prop?.GetValue(linkDto) as DateTime?;
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

