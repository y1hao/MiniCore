using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniCore.Reference.Controllers;
using MiniCore.Reference.Data;
using MiniCore.Reference.Models;

namespace MiniCore.Reference.Tests.Controllers;

public class AdminControllerTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly AdminController _controller;

    public AdminControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _controller = new AdminController(_context);

        // Setup mock HTTP context
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("localhost:5000");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
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
        _context.ShortLinks.AddRange(link1, link2, link3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<ShortLinkDto>>(viewResult.Model);
        var linksList = model.ToList();
        Assert.Equal(3, linksList.Count);
        // Verify ordering: most recent first
        Assert.Equal("ghi789", linksList[0].ShortCode);
        Assert.Equal("def456", linksList[1].ShortCode);
        Assert.Equal("abc123", linksList[2].ShortCode);
    }

    [Fact]
    public async Task Index_ReturnsViewResult_WithEmptyList_WhenNoLinksExist()
    {
        // Act
        var result = await _controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<ShortLinkDto>>(viewResult.Model);
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
        var model = Assert.IsAssignableFrom<IEnumerable<ShortLinkDto>>(viewResult.Model);
        var linkDto = model.First();
        Assert.Equal("https://localhost:5000/test123", linkDto.ShortUrl);
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
        var model = Assert.IsAssignableFrom<IEnumerable<ShortLinkDto>>(viewResult.Model);
        var linkDto = model.First();
        Assert.NotNull(linkDto.ExpiresAt);
        Assert.Equal(expiresAt, linkDto.ExpiresAt.Value, TimeSpan.FromSeconds(1));
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
        var model = Assert.IsAssignableFrom<IEnumerable<ShortLinkDto>>(viewResult.Model);
        var linkDto = model.First();
        Assert.Null(linkDto.ExpiresAt);
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

