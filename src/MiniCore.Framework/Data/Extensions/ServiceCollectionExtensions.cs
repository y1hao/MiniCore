using MiniCore.Framework.Data;
using MiniCore.Framework.DependencyInjection;
using MiniCore.Framework.Logging;

namespace MiniCore.Framework.Data.Extensions;

/// <summary>
/// Extension methods for adding DbContext to the service collection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers a DbContext with the service collection.
    /// </summary>
    /// <typeparam name="TContext">The type of the DbContext.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="optionsAction">The action to configure the DbContext options.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddDbContext<TContext>(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder>? optionsAction = null)
        where TContext : DbContext
    {
        // Create DbContextOptions
        var optionsBuilder = new DbContextOptionsBuilder<TContext>();
        optionsAction?.Invoke(optionsBuilder);
        var options = optionsBuilder.Options;

        // Register DbContextOptions<TContext> as singleton
        services.AddSingleton<DbContextOptions<TContext>>(options);

        // Register DbContext as scoped with factory
        // This allows us to get ILoggerFactory from the service provider when creating the instance
        services.AddScoped<TContext>(serviceProvider =>
        {
            var optionsInstance = serviceProvider.GetRequiredService<DbContextOptions<TContext>>();
            // Get logger factory from service provider and update options if not already set
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            if (loggerFactory != null && optionsInstance.LoggerFactory == null)
            {
                optionsInstance.LoggerFactory = loggerFactory;
            }
            return (TContext)(Activator.CreateInstance(typeof(TContext), optionsInstance)
                ?? throw new InvalidOperationException($"Failed to create instance of {typeof(TContext)}"));
        });

        return services;
    }
}

