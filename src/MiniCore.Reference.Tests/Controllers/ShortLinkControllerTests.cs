using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MiniCore.Reference.Controllers;
using MiniCore.Reference.Data;
using MiniCore.Reference.Models;
using Moq;
using System.Security.Claims;

namespace MiniCore.Reference.Tests.Controllers;

public class ShortLinkControllerTests : IDisposable
{
    private readonly Mock<ILogger<ShortLinkController>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly AppDbContext _context;
    private readonly ShortLinkController _controller;

    public ShortLinkControllerTests()
    {
        _mockLogger = new Mock<ILogger<ShortLinkController>>();
        _mockConfiguration = new Mock<IConfiguration>();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _controller = new ShortLinkController(_context, _mockLogger.Object, _mockConfiguration.Object);

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
    public async Task GetLinks_ReturnsOkResult_WithLinks()
    {
        // Arrange
        var link1 = new ShortLink { ShortCode = "abc123", OriginalUrl = "https://example.com", CreatedAt = DateTime.UtcNow };
        var link2 = new ShortLink { ShortCode = "def456", OriginalUrl = "https://test.com", CreatedAt = DateTime.UtcNow };
        _context.ShortLinks.AddRange(link1, link2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetLinks();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var links = Assert.IsAssignableFrom<IEnumerable<ShortLinkDto>>(okResult.Value);
        Assert.Equal(2, links.Count());
    }

    [Fact]
    public async Task CreateLink_WithValidUrl_ReturnsCreatedResult()
    {
        // Arrange
        var configurationSection = new Mock<IConfigurationSection>();
        configurationSection.Setup(s => s.Value).Returns("30");
        _mockConfiguration.Setup(c => c.GetSection("LinkCleanup:DefaultExpirationDays"))
            .Returns(configurationSection.Object);
        _mockConfiguration.Setup(c => c["LinkCleanup:DefaultExpirationDays"]).Returns("30");

        var request = new CreateShortLinkRequest
        {
            OriginalUrl = "https://example.com"
        };

        // Act
        var result = await _controller.CreateLink(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto = Assert.IsType<ShortLinkDto>(createdResult.Value);
        Assert.Equal(request.OriginalUrl, dto.OriginalUrl);
        Assert.NotNull(dto.ShortCode);
        Assert.NotEmpty(dto.ShortCode);
    }

    [Fact]
    public async Task CreateLink_WithInvalidUrl_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateShortLinkRequest
        {
            OriginalUrl = "not-a-valid-url"
        };

        // Act
        var result = await _controller.CreateLink(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateLink_WithCustomShortCode_ReturnsCreatedResult()
    {
        // Arrange
        var configurationSection = new Mock<IConfigurationSection>();
        configurationSection.Setup(s => s.Value).Returns("30");
        _mockConfiguration.Setup(c => c.GetSection("LinkCleanup:DefaultExpirationDays"))
            .Returns(configurationSection.Object);
        _mockConfiguration.Setup(c => c["LinkCleanup:DefaultExpirationDays"]).Returns("30");

        var request = new CreateShortLinkRequest
        {
            OriginalUrl = "https://example.com",
            ShortCode = "my-custom-link"
        };

        // Act
        var result = await _controller.CreateLink(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto = Assert.IsType<ShortLinkDto>(createdResult.Value);
        Assert.Equal("my-custom-link", dto.ShortCode);
        Assert.Equal(request.OriginalUrl, dto.OriginalUrl);
    }

    [Fact]
    public async Task CreateLink_WithDuplicateCustomShortCode_ReturnsConflict()
    {
        // Arrange
        var configurationSection = new Mock<IConfigurationSection>();
        configurationSection.Setup(s => s.Value).Returns("30");
        _mockConfiguration.Setup(c => c.GetSection("LinkCleanup:DefaultExpirationDays"))
            .Returns(configurationSection.Object);
        _mockConfiguration.Setup(c => c["LinkCleanup:DefaultExpirationDays"]).Returns("30");

        // Create first link with custom short code
        var existingLink = new ShortLink 
        { 
            ShortCode = "existing-code", 
            OriginalUrl = "https://example.com", 
            CreatedAt = DateTime.UtcNow 
        };
        _context.ShortLinks.Add(existingLink);
        await _context.SaveChangesAsync();

        var request = new CreateShortLinkRequest
        {
            OriginalUrl = "https://another-example.com",
            ShortCode = "existing-code"
        };

        // Act
        var result = await _controller.CreateLink(request);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.NotNull(conflictResult.Value);
    }

    [Fact]
    public async Task CreateLink_WithInvalidCustomShortCodeFormat_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateShortLinkRequest
        {
            OriginalUrl = "https://example.com",
            ShortCode = "invalid code!" // Contains space and special character
        };

        // Act
        var result = await _controller.CreateLink(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task CreateLink_WithApiAsShortCode_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateShortLinkRequest
        {
            OriginalUrl = "https://example.com",
            ShortCode = "api" // Reserved word - should be rejected
        };

        // Act
        var result = await _controller.CreateLink(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
        var errorMessage = badRequestResult.Value.ToString();
        Assert.Contains("reserved", errorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateLink_WithAdminAsShortCode_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateShortLinkRequest
        {
            OriginalUrl = "https://example.com",
            ShortCode = "admin" // Reserved word - should be rejected
        };

        // Act
        var result = await _controller.CreateLink(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
        var errorMessage = badRequestResult.Value.ToString();
        Assert.Contains("reserved", errorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateLink_WithCustomShortCodeTooLong_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateShortLinkRequest
        {
            OriginalUrl = "https://example.com",
            ShortCode = new string('a', 21) // 21 characters, exceeds max of 20
        };

        // Act
        var result = await _controller.CreateLink(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task DeleteLink_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var link = new ShortLink { ShortCode = "abc123", OriginalUrl = "https://example.com", CreatedAt = DateTime.UtcNow };
        _context.ShortLinks.Add(link);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.DeleteLink(link.Id);

        // Assert
        Assert.IsType<NoContentResult>(result);
        Assert.False(await _context.ShortLinks.AnyAsync(l => l.Id == link.Id));
    }

    [Fact]
    public async Task DeleteLink_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var result = await _controller.DeleteLink(999);

        // Assert
        Assert.IsType<NotFoundResult>(result);
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

