namespace MiniCore.Framework.Hosting;

/// <summary>
/// Provides information about the web hosting environment an application is running in.
/// </summary>
public interface IWebHostEnvironment
{
    /// <summary>
    /// Gets or sets the name of the environment. The host automatically sets this property to the value of the
    /// "ASPNETCORE_ENVIRONMENT" environment variable, or "Production" if the environment variable is not set.
    /// </summary>
    string EnvironmentName { get; set; }

    /// <summary>
    /// Gets or sets the absolute path to the directory that contains the application content files.
    /// </summary>
    string ContentRootPath { get; set; }

    /// <summary>
    /// Checks if the current host environment name is "Development".
    /// </summary>
    /// <returns>True if the environment name is "Development", otherwise false.</returns>
    bool IsDevelopment();

    /// <summary>
    /// Compares the current host environment name against the specified value.
    /// </summary>
    /// <param name="environmentName">Environment name to validate against.</param>
    /// <returns>True if the specified name is the same as the current environment, otherwise false.</returns>
    bool IsEnvironment(string environmentName);
}

