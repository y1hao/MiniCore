using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using MiniCore.Framework.Configuration;
using MiniCore.Framework.Configuration.Abstractions;

namespace MiniCore.Web;

/// <summary>
/// Adapter that wraps our custom IConfigurationRoot and implements Microsoft's IConfiguration interface.
/// This allows our custom configuration to be used wherever Microsoft's IConfiguration is expected.
/// TODO: REMOVE IN PHASE 4 (Host Abstraction) when we implement our own HostBuilder.
/// </summary>
public class ConfigurationAdapter : Microsoft.Extensions.Configuration.IConfiguration
{
    private readonly MiniCore.Framework.Configuration.Abstractions.IConfigurationRoot _configuration;

    public ConfigurationAdapter(MiniCore.Framework.Configuration.Abstractions.IConfigurationRoot configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public string? this[string key]
    {
        get => _configuration[key];
        set => _configuration[key] = value;
    }

    public Microsoft.Extensions.Configuration.IConfigurationSection GetSection(string key)
    {
        var section = _configuration.GetSection(key);
        return new ConfigurationSectionAdapter(section);
    }

    public IEnumerable<Microsoft.Extensions.Configuration.IConfigurationSection> GetChildren()
    {
        return _configuration.GetChildren().Select(s => new ConfigurationSectionAdapter(s));
    }

    public Microsoft.Extensions.Primitives.IChangeToken GetReloadToken()
    {
        var token = _configuration.GetReloadToken();
        return new ChangeTokenAdapter(token);
    }
}

public class ConfigurationSectionAdapter : Microsoft.Extensions.Configuration.IConfigurationSection
{
    private readonly MiniCore.Framework.Configuration.Abstractions.IConfigurationSection _section;

    public ConfigurationSectionAdapter(MiniCore.Framework.Configuration.Abstractions.IConfigurationSection section)
    {
        _section = section ?? throw new ArgumentNullException(nameof(section));
    }

    public string Key => _section.Key;
    public string Path => _section.Path;
    public string? Value
    {
        get => _section.Value;
        set => _section.Value = value;
    }

    public string? this[string key]
    {
        get => _section[key];
        set => _section[key] = value;
    }

    public Microsoft.Extensions.Configuration.IConfigurationSection GetSection(string key)
    {
        var section = _section.GetSection(key);
        return new ConfigurationSectionAdapter(section);
    }

    public IEnumerable<Microsoft.Extensions.Configuration.IConfigurationSection> GetChildren()
    {
        return _section.GetChildren().Select(s => new ConfigurationSectionAdapter(s));
    }

    public Microsoft.Extensions.Primitives.IChangeToken GetReloadToken()
    {
        var token = _section.GetReloadToken();
        return new ChangeTokenAdapter(token);
    }
}

public class ChangeTokenAdapter : Microsoft.Extensions.Primitives.IChangeToken
{
    private readonly MiniCore.Framework.Configuration.Abstractions.IChangeToken _token;

    public ChangeTokenAdapter(MiniCore.Framework.Configuration.Abstractions.IChangeToken token)
    {
        _token = token ?? throw new ArgumentNullException(nameof(token));
    }

    public bool HasChanged => _token.HasChanged;
    public bool ActiveChangeCallbacks => _token.ActiveChangeCallbacks;

    public IDisposable RegisterChangeCallback(Action<object?> callback, object? state)
    {
        return _token.RegisterChangeCallback(callback, state);
    }
}

