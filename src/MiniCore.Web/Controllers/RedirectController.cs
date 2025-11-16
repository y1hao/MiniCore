using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniCore.Web.Data;
using MiniCore.Web.Models;

namespace MiniCore.Web.Controllers;

public class RedirectController(AppDbContext context, ILogger<RedirectController> logger) : ControllerBase
{
    private readonly AppDbContext _context = context;
    private readonly ILogger<RedirectController> _logger = logger;

    public async Task<IActionResult> RedirectToUrl(string path)
    {
        // Extract shortCode from path (MapFallbackToController with {*path} pattern passes the entire unmatched path)
        var shortCode = path?.TrimStart('/').Split('/')[0];
        
        if (string.IsNullOrEmpty(shortCode))
        {
            return NotFound();
        }
        
        var link = await _context.ShortLinks
            .FirstOrDefaultAsync(l => l.ShortCode == shortCode);

        if (link == null)
        {
            _logger.LogWarning("Short code not found: {ShortCode}", shortCode);
            return NotFound();
        }

        if (link.ExpiresAt.HasValue && link.ExpiresAt.Value < DateTime.UtcNow)
        {
            _logger.LogWarning("Short code expired: {ShortCode}", shortCode);
            return NotFound();
        }

        _logger.LogInformation("Redirecting {ShortCode} -> {OriginalUrl}", shortCode, link.OriginalUrl);

        return Redirect(link.OriginalUrl);
    }
}

