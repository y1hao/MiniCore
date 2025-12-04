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

    [HttpGet("/")]
    [HttpGet("/")]
    [HttpGet("/admin")]
    public async Task<IActionResult> Index()
    {
        var links = await _context.ShortLinks
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();

        var dtos = links.Select(l => new
        {
            Id = l.Id,
            ShortCode = l.ShortCode,
            OriginalUrl = l.OriginalUrl,
            CreatedAt = l.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
            ExpiresAt = l.ExpiresAt, // Keep as DateTime? for test compatibility
            ExpiresAtFormatted = l.ExpiresAt.HasValue ? l.ExpiresAt.Value.ToString("yyyy-MM-dd HH:mm") : null,
            ShortUrl = $"{Request.Scheme}://{Request.Host}/{l.ShortCode}"
        }).ToList();

        ViewData["Title"] = "Admin - URL Shortener";
        return View(dtos);
    }
}

