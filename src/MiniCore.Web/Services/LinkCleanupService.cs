using MiniCore.Framework.Configuration.Abstractions;
using MiniCore.Framework.Data.Extensions;
using MiniCore.Framework.DependencyInjection;
using MiniCore.Framework.Logging;
using MiniHostedService = MiniCore.Framework.Hosting.IHostedService;
using MiniCore.Web.Data;

namespace MiniCore.Web.Services;

public class LinkCleanupService : MiniHostedService
{
    private readonly MiniCore.Framework.DependencyInjection.IServiceProvider _serviceProvider;
    private readonly MiniCore.Framework.Logging.ILogger<LinkCleanupService> _logger;
    private readonly MiniCore.Framework.Configuration.Abstractions.IConfiguration _configuration;
    private readonly TimeSpan _interval;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _executingTask;

    public LinkCleanupService(
        MiniCore.Framework.DependencyInjection.IServiceProvider serviceProvider,
        MiniCore.Framework.Logging.ILogger<LinkCleanupService> logger,
        MiniCore.Framework.Configuration.Abstractions.IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
        
        var intervalHoursStr = _configuration["LinkCleanup:IntervalHours"];
        var intervalHours = int.TryParse(intervalHoursStr, out var hours) ? hours : 1;
        _interval = TimeSpan.FromHours(intervalHours);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _executingTask = ExecuteAsync(_cancellationTokenSource.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_executingTask == null)
        {
            return;
        }

        _cancellationTokenSource?.Cancel();
        await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite, cancellationToken));
        cancellationToken.ThrowIfCancellationRequested();
    }

    private async Task ExecuteAsync(CancellationToken stoppingToken)
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
        var scopeFactory = _serviceProvider.GetService<MiniCore.Framework.DependencyInjection.IServiceScopeFactory>();
        if (scopeFactory == null)
        {
            _logger.LogError("IServiceScopeFactory is not available");
            return;
        }

        using var scope = scopeFactory.CreateScope();
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

