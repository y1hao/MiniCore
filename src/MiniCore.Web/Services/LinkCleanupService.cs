using Microsoft.EntityFrameworkCore;
using MiniCore.Web.Data;

namespace MiniCore.Web.Services;

public class LinkCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LinkCleanupService> _logger;
    private readonly IConfiguration _configuration;
    private readonly TimeSpan _interval;

    public LinkCleanupService(
        IServiceProvider serviceProvider,
        ILogger<LinkCleanupService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
        
        var intervalHours = _configuration.GetValue<int>("LinkCleanup:IntervalHours", 1);
        _interval = TimeSpan.FromHours(intervalHours);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("LinkCleanupService started. Cleanup interval: {Interval}", _interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredLinks(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during link cleanup");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("LinkCleanupService stopped");
    }

    public async Task CleanupExpiredLinks(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var expiredLinks = await context.ShortLinks
            .Where(l => l.ExpiresAt.HasValue && l.ExpiresAt.Value < DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        if (expiredLinks.Count > 0)
        {
            context.ShortLinks.RemoveRange(expiredLinks);
            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Cleaned up {Count} expired link(s). Removed short codes: {ShortCodes}",
                expiredLinks.Count,
                string.Join(", ", expiredLinks.Select(l => l.ShortCode)));
        }
        else
        {
            _logger.LogDebug("No expired links found to clean up");
        }
    }
}

