using Microsoft.EntityFrameworkCore;
using MiniCore.Framework.Configuration;
using MiniCore.Framework.Configuration.Abstractions;
using MiniCore.Framework.DependencyInjection;

namespace MiniCore.Web;

/// <summary>
/// Extension methods for Entity Framework Core integration with MiniCore.Framework DI.
/// </summary>
public static class EntityFrameworkExtensions
{
    /// <summary>
    /// Registers a DbContext with the service collection.
    /// </summary>
    /// <typeparam name="TContext">The type of the DbContext.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="optionsAction">The action to configure the DbContext options.</param>
    /// <returns>The service collection.</returns>
    public static MiniCore.Framework.DependencyInjection.IServiceCollection AddDbContext<TContext>(
        this MiniCore.Framework.DependencyInjection.IServiceCollection services,
        Action<DbContextOptionsBuilder>? optionsAction = null)
        where TContext : DbContext
    {
        // Create DbContextOptions
        var optionsBuilder = new DbContextOptionsBuilder<TContext>();
        optionsAction?.Invoke(optionsBuilder);
        var options = optionsBuilder.Options;

        // Register DbContextOptions<TContext> as singleton
        services.Add(MiniCore.Framework.DependencyInjection.ServiceDescriptor.Describe(
            typeof(DbContextOptions<TContext>), 
            options, 
            MiniCore.Framework.DependencyInjection.ServiceLifetime.Singleton));

        // Register DbContext as scoped
        services.Add(MiniCore.Framework.DependencyInjection.ServiceDescriptor.Describe(
            typeof(TContext), 
            serviceProvider =>
            {
                var optionsInstance = serviceProvider.GetRequiredService<DbContextOptions<TContext>>();
                return Activator.CreateInstance(typeof(TContext), optionsInstance) 
                    ?? throw new InvalidOperationException($"Failed to create instance of {typeof(TContext)}");
            }, 
            MiniCore.Framework.DependencyInjection.ServiceLifetime.Scoped));

        return services;
    }

    /// <summary>
    /// Registers a DbContext with the service collection using a connection string from configuration.
    /// </summary>
    /// <typeparam name="TContext">The type of the DbContext.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="connectionStringName">The name of the connection string in configuration.</param>
    /// <param name="optionsAction">Optional additional configuration for the DbContext options.</param>
    /// <returns>The service collection.</returns>
    public static MiniCore.Framework.DependencyInjection.IServiceCollection AddDbContext<TContext>(
        this MiniCore.Framework.DependencyInjection.IServiceCollection services,
        MiniCore.Framework.Configuration.Abstractions.IConfiguration configuration,
        string connectionStringName,
        Action<DbContextOptionsBuilder>? optionsAction = null)
        where TContext : DbContext
    {
        var connectionString = MiniCore.Framework.Configuration.ConfigurationExtensions.GetConnectionString(configuration, connectionStringName);
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException($"Connection string '{connectionStringName}' not found in configuration.");
        }

        return services.AddDbContext<TContext>(options =>
        {
            optionsAction?.Invoke(options);
            // Note: UseSqlite will be called by the caller, so we don't need to call it here
        });
    }
}

