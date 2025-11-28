using Microsoft.EntityFrameworkCore;
using MiniCore.Framework.Logging;
using MiniCore.Framework.Mvc.Abstractions;
using MiniCore.Framework.Mvc.Controllers;
using MiniCore.Framework.Routing.Attributes;
using MiniCore.Web.Data;
using MiniCore.Web.Models;

namespace MiniCore.Web.Controllers;

[Route("{*path}")]
public class RedirectController(AppDbContext context, MiniCore.Framework.Logging.ILogger<RedirectController> logger) : ControllerBase
{
    private readonly AppDbContext _context = context;
    private readonly MiniCore.Framework.Logging.ILogger<RedirectController> _logger = logger;

    public async Task<IActionResult> RedirectToUrl(string path)
    {
        // Extract shortCode from path (MapFallbackToController with {*path} pattern passes the entire unmatched path)
        var pathSegments = path?.TrimStart('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        
        if (pathSegments == null || pathSegments.Length == 0)
        {
            return NotFound();
        }
        
        var shortCode = pathSegments[0];
        
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

        // Preserve additional path segments after the short code
        var redirectUrl = link.OriginalUrl;
        if (pathSegments.Length > 1)
        {
            var additionalPath = string.Join("/", pathSegments.Skip(1));
            // Ensure proper URL joining: remove trailing slash from original URL if present, then add the additional path
            redirectUrl = redirectUrl.TrimEnd('/') + "/" + additionalPath;
        }

        _logger.LogInformation("Redirecting {ShortCode} -> {RedirectUrl}", shortCode, redirectUrl);

        return Redirect(redirectUrl);
    }
}

