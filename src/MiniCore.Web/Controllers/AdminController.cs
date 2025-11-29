using MiniCore.Framework.Data.Extensions;
using MiniCore.Framework.Mvc.Abstractions;
using MiniCore.Framework.Mvc.Controllers;
using MiniCore.Framework.Routing.Attributes;
using MiniCore.Web.Data;
using MiniCore.Web.Models;

namespace MiniCore.Web.Controllers;

public class AdminController(AppDbContext context) : Controller
{
    private readonly AppDbContext _context = context;

    [HttpGet("/admin")]
    public async Task<IActionResult> Index()
    {
        var links = await _context.ShortLinks
            .OrderByDescending(l => l.CreatedAt)
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

        // TODO: Implement view rendering when templating is available
        return NotFound();
    }
}

