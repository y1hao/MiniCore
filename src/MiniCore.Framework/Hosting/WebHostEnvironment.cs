namespace MiniCore.Framework.Hosting;

/// <summary>
/// Provides information about the web hosting environment an application is running in.
/// </summary>
public class WebHostEnvironment : IWebHostEnvironment
{
    private string _environmentName = "Production";
    private string _contentRootPath = string.Empty;

    /// <summary>
    /// Gets or sets the name of the environment. The host automatically sets this property to the value of the
    /// "ASPNETCORE_ENVIRONMENT" environment variable, or "Production" if the environment variable is not set.
    /// </summary>
    public string EnvironmentName
    {
        get => _environmentName;
        set => _environmentName = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Gets or sets the absolute path to the directory that contains the application content files.
    /// </summary>
    public string ContentRootPath
    {
        get => _contentRootPath;
        set => _contentRootPath = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Checks if the current host environment name is "Development".
    /// </summary>
    /// <returns>True if the environment name is "Development", otherwise false.</returns>
    public bool IsDevelopment()
    {
        return string.Equals(EnvironmentName, "Development", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Compares the current host environment name against the specified value.
    /// </summary>
    /// <param name="environmentName">Environment name to validate against.</param>
    /// <returns>True if the specified name is the same as the current environment, otherwise false.</returns>
    public bool IsEnvironment(string environmentName)
    {
        if (string.IsNullOrEmpty(environmentName))
        {
            throw new ArgumentException("Environment name cannot be null or empty.", nameof(environmentName));
        }

        return string.Equals(EnvironmentName, environmentName, StringComparison.OrdinalIgnoreCase);
    }
}

