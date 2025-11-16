using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniCore.Web.Data;
using MiniCore.Web.Models;

namespace MiniCore.Web.Controllers;

public class AdminController : Controller
{
    private readonly AppDbContext _context;

    public AdminController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("/admin")]
    public async Task<IActionResult> Index()
    {
        var links = await _context.ShortLinks
            .OrderByDescending(l => l.CreatedAt)
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

        return View(links);
    }
}

