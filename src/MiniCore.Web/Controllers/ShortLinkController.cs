using MiniCore.Framework.Configuration.Abstractions;
using MiniCore.Framework.Data.Extensions;
using MiniCore.Framework.Logging;
using MiniCore.Framework.Mvc.Abstractions;
using MiniCore.Framework.Mvc.Controllers;
using MiniCore.Framework.Mvc.ModelBinding;
using MiniCore.Framework.Routing.Attributes;
using MiniCore.Web.Data;
using MiniCore.Web.Models;
using System.Security.Cryptography;
using System.Text;

namespace MiniCore.Web.Controllers;

[Route("api/links")]
public class ShortLinkController(AppDbContext context, MiniCore.Framework.Logging.ILogger<ShortLinkController> logger, MiniCore.Framework.Configuration.Abstractions.IConfiguration configuration) : ControllerBase
{
    private readonly AppDbContext _context = context;
    private readonly MiniCore.Framework.Logging.ILogger<ShortLinkController> _logger = logger;
    private readonly MiniCore.Framework.Configuration.Abstractions.IConfiguration _configuration = configuration;

    [HttpGet]
    public async Task<IActionResult> GetLinks([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var links = await _context.ShortLinks
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = links.Select(l => new ShortLinkDto
        {
            Id = l.Id,
            ShortCode = l.ShortCode,
            OriginalUrl = l.OriginalUrl,
            CreatedAt = l.CreatedAt,
            ExpiresAt = l.ExpiresAt,
            ShortUrl = $"{Request.Scheme}://{Request.Host}/{l.ShortCode}"
        }).ToList();

        return Ok(dtos);
    }

    [HttpPost]
    public async Task<IActionResult> CreateLink([FromBody] CreateShortLinkRequest request)
    {
        _logger.LogInformation("CreateLink called with request: {Request}", request != null ? "not null" : "null");
        
        if (request == null || string.IsNullOrWhiteSpace(request.OriginalUrl))
        {
            _logger.LogWarning("CreateLink failed: Invalid request - OriginalUrl is required");
            return BadRequest(new { error = "OriginalUrl is required" });
        }
        
        _logger.LogInformation("Creating short link for URL: {OriginalUrl}", request.OriginalUrl);

        if (!Uri.TryCreate(request.OriginalUrl, UriKind.Absolute, out var uri))
        {
            return BadRequest(new { error = "Invalid URL format" });
        }

        string shortCode;
        
        if (!string.IsNullOrWhiteSpace(request.ShortCode))
        {
            // User provided a custom short code
            shortCode = request.ShortCode.Trim();
            
            // Validate format: alphanumeric, hyphens, underscores only, 1-20 characters
            if (shortCode.Length < 1 || shortCode.Length > 20)
            {
                return BadRequest(new { error = "Short code must be between 1 and 20 characters" });
            }
            
            if (!System.Text.RegularExpressions.Regex.IsMatch(shortCode, @"^[a-zA-Z0-9_-]+$"))
            {
                return BadRequest(new { error = "Short code can only contain letters, numbers, hyphens, and underscores" });
            }
            
            // Check for reserved words that conflict with application routes
            var reservedWords = new[] { "api", "admin" };
            if (reservedWords.Contains(shortCode, StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest(new { error = $"Short code '{shortCode}' is reserved and cannot be used" });
            }
            
            // Check for uniqueness
            if (await _context.ShortLinks.AnyAsync(l => l.ShortCode == shortCode))
            {
                return Conflict(new { error = $"Short code '{shortCode}' is already in use" });
            }
        }
        else
        {
            // Generate a short code automatically
            shortCode = GenerateShortCode(request.OriginalUrl);
            
            // Ensure uniqueness
            while (await _context.ShortLinks.AnyAsync(l => l.ShortCode == shortCode))
            {
                shortCode = GenerateShortCode(request.OriginalUrl + Guid.NewGuid().ToString());
            }
        }

        var defaultExpirationDaysStr = _configuration["LinkCleanup:DefaultExpirationDays"];
        var defaultExpirationDays = int.TryParse(defaultExpirationDaysStr, out var days) ? days : 30;
        var expiresAt = request.ExpiresAt ?? DateTime.UtcNow.AddDays(defaultExpirationDays);

        var link = new ShortLink
        {
            ShortCode = shortCode,
            OriginalUrl = request.OriginalUrl,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt
        };

        _context.ShortLinks.Add(link);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created short link: {ShortCode} -> {OriginalUrl}", shortCode, request.OriginalUrl);

        var dto = new ShortLinkDto
        {
            Id = link.Id,
            ShortCode = link.ShortCode,
            OriginalUrl = link.OriginalUrl,
            CreatedAt = link.CreatedAt,
            ExpiresAt = link.ExpiresAt,
            ShortUrl = $"{Request.Scheme}://{Request.Host}/{link.ShortCode}"
        };

        var location = $"{Request.Scheme}://{Request.Host}/api/links/{link.Id}";
        return Created(location, dto);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteLink(int id)
    {
        var link = await _context.ShortLinks.FindAsync(new object[] { id });
        if (link == null)
        {
            return NotFound();
        }

        _context.ShortLinks.Remove(link);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted short link: {ShortCode}", link.ShortCode);

        return NoContent();
    }

    private static string GenerateShortCode(string input)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input + DateTime.UtcNow.Ticks));
        var base64 = Convert.ToBase64String(hash)
            .Replace("+", "")
            .Replace("/", "")
            .Replace("=", "");
        return base64.Substring(0, Math.Min(8, base64.Length));
    }
}

