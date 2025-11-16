using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniCore.Web.Data;
using MiniCore.Web.Models;
using System.Security.Cryptography;
using System.Text;

namespace MiniCore.Web.Controllers;

[ApiController]
[Route("api/links")]
public class ShortLinkController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<ShortLinkController> _logger;
    private readonly IConfiguration _configuration;

    public ShortLinkController(AppDbContext context, ILogger<ShortLinkController> logger, IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ShortLinkDto>>> GetLinks([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var links = await _context.ShortLinks
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new ShortLinkDto
            {
                Id = l.Id,
                ShortCode = l.ShortCode,
                OriginalUrl = l.OriginalUrl,
                CreatedAt = l.CreatedAt,
                ExpiresAt = l.ExpiresAt,
                ShortUrl = $"{Request.Scheme}://{Request.Host}/{l.ShortCode}"
            })
            .ToListAsync();

        return Ok(links);
    }

    [HttpPost]
    public async Task<ActionResult<ShortLinkDto>> CreateLink([FromBody] CreateShortLinkRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors.Select(e => e.ErrorMessage))
                .ToList();
            return BadRequest(new { error = string.Join(", ", errors), errors = ModelState });
        }

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

        var defaultExpirationDays = _configuration.GetValue<int>("LinkCleanup:DefaultExpirationDays", 30);
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

        return CreatedAtAction(nameof(GetLinks), new { id = link.Id }, dto);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteLink(int id)
    {
        var link = await _context.ShortLinks.FindAsync(id);
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

