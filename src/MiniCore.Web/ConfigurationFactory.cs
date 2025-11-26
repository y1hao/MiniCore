using MiniCore.Framework.Configuration;

namespace MiniCore.Web;

/// <summary>
/// Factory for creating custom configuration from appsettings.json and environment variables.
/// This bridges our custom configuration framework with ASP.NET Core.
/// TODO: REMOVE IN PHASE 4 (Host Abstraction) when we implement our own HostBuilder.
/// </summary>
public static class ConfigurationFactory
{
    /// <summary>
    /// Creates a configuration root from appsettings.json and environment variables.
    /// </summary>
    /// <param name="basePath">The base path for configuration files.</param>
    /// <param name="environmentName">The environment name (e.g., "Development").</param>
    /// <returns>An IConfigurationRoot instance.</returns>
    public static MiniCore.Framework.Configuration.Abstractions.IConfigurationRoot CreateConfiguration(string basePath, string? environmentName = null)
    {
        var builder = new MiniCore.Framework.Configuration.ConfigurationBuilder();

        // Add appsettings.json
        var appsettingsPath = Path.Combine(basePath, "appsettings.json");
        if (File.Exists(appsettingsPath))
        {
            builder.AddJsonFile(appsettingsPath, optional: false, reloadOnChange: false);
        }

        // Add appsettings.{Environment}.json if environment is specified
        if (!string.IsNullOrEmpty(environmentName))
        {
            var envAppsettingsPath = Path.Combine(basePath, $"appsettings.{environmentName}.json");
            if (File.Exists(envAppsettingsPath))
            {
                builder.AddJsonFile(envAppsettingsPath, optional: true, reloadOnChange: false);
            }
        }

        // Add environment variables (they override JSON values)
        builder.AddEnvironmentVariables();

        return builder.Build();
    }
}

