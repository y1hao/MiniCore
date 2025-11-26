# Chapter 2: Configuration Framework ✅

## Overview

Phase 2 implemented a minimal Configuration framework to replace `Microsoft.Extensions.Configuration`. This provides a hierarchical key-value store for application settings, supporting multiple configuration sources (JSON files and environment variables) with proper precedence handling.

**Status:** ✅ Complete

## Goals

- Implement core configuration interfaces matching Microsoft's API surface
- Support hierarchical key-value storage with colon-separated keys (e.g., `"A:B:C"`)
- Enable multiple configuration sources (JSON files, environment variables)
- Provide `IConfigurationBuilder` to compose multiple sources
- Support configuration sections (`IConfigurationSection`)
- Implement POCO binding (`config.Bind<T>()`) for mapping configuration to objects
- Support configuration reload tokens (`IChangeToken`) for change notifications

## Key Requirements

### Interfaces to Implement

1. **IConfiguration**
   - `this[string key]` - Get/set configuration value by key
   - `GetSection(string key)` - Get a configuration section
   - `GetChildren()` - Get child configuration sections
   - `GetReloadToken()` - Get change token for reload notifications

2. **IConfigurationRoot**
   - Extends `IConfiguration`
   - `Reload()` - Reload configuration from all providers
   - Represents the root of the configuration hierarchy

3. **IConfigurationSection**
   - Extends `IConfiguration`
   - `Key` - Section key name
   - `Path` - Full path to section (e.g., `"MySection:Nested"`)
   - `Value` - Direct value of the section (if it has one)

4. **IConfigurationBuilder**
   - `Properties` - Dictionary for storing builder properties
   - `Sources` - List of configuration sources
   - `Add(IConfigurationSource source)` - Add a configuration source
   - `Build()` - Build `IConfigurationRoot` from sources

5. **IConfigurationSource**
   - `Build(IConfigurationBuilder builder)` - Build provider from source

6. **IConfigurationProvider**
   - `TryGet(string key, out string? value)` - Try to get value by key
   - `Set(string key, string? value)` - Set a configuration value
   - `GetChildKeys(IEnumerable<string> earlierKeys, string? parentPath)` - Get child keys
   - `Load()` - Load configuration from source
   - `GetReloadToken()` - Get change token for this provider

7. **IChangeToken**
   - `HasChanged` - Whether configuration has changed
   - `ActiveChangeCallbacks` - Whether callbacks are supported
   - `RegisterChangeCallback(Action<object?> callback, object? state)` - Register callback

### Core Features

1. **Hierarchical Configuration**
   - Keys use colon separator: `"ConnectionStrings:DefaultConnection"`
   - Sections can be nested: `"MySection:Nested:Key"`
   - Sections can have both a value and children

2. **Configuration Sources**
   - **JSON Files**: Read from `appsettings.json` and `appsettings.{Environment}.json`
   - **Environment Variables**: Read from process environment variables
   - Support for `reloadOnChange` (future: file watching)

3. **Source Precedence**
   - Later sources override earlier ones
   - Environment variables override JSON files (when added after JSON)
   - Providers are queried in reverse order

4. **POCO Binding**
   - `Bind<T>(T instance)` - Bind configuration section to object instance
   - `GetValue<T>(string key)` - Get typed value from configuration
   - Recursive binding for nested objects
   - Type conversion for primitives

5. **Configuration Reload**
   - `IChangeToken` mechanism for change notifications
   - `Reload()` method to trigger reload from providers
   - Support for registering callbacks on configuration changes

## Architecture

```
MiniCore.Framework/
└── Configuration/
    ├── Abstractions/
    │   ├── IConfiguration.cs              # Core configuration interface
    │   ├── IConfigurationRoot.cs          # Root configuration interface
    │   ├── IConfigurationSection.cs       # Section interface
    │   ├── IConfigurationBuilder.cs       # Builder interface
    │   ├── IConfigurationSource.cs        # Source interface
    │   ├── IConfigurationProvider.cs      # Provider interface
    │   └── IChangeToken.cs                 # Change token interface
    ├── ConfigurationRoot.cs                # Root implementation
    ├── ConfigurationSection.cs             # Section implementation
    ├── ConfigurationBuilder.cs             # Builder implementation
    ├── ConfigurationPath.cs                # Path manipulation utilities
    ├── ConfigurationReloadToken.cs         # Change token implementation
    ├── Json/
    │   ├── JsonConfigurationSource.cs      # JSON source
    │   └── JsonConfigurationProvider.cs    # JSON provider
    ├── EnvironmentVariables/
    │   ├── EnvironmentVariablesConfigurationSource.cs  # Env var source
    │   └── EnvironmentVariablesConfigurationProvider.cs  # Env var provider
    └── Extensions/
        ├── ConfigurationBuilderExtensions.cs  # Builder extension methods
        └── ConfigurationExtensions.cs        # Configuration extension methods (Bind, GetValue)
```

## Implementation Summary

Phase 2 successfully implemented all core components:

### ✅ Core Types and Interfaces

- **IConfiguration.cs** - Core configuration access interface
- **IConfigurationRoot.cs** - Root configuration with reload capability
- **IConfigurationSection.cs** - Hierarchical section interface
- **IConfigurationBuilder.cs** - Builder pattern for composing sources
- **IConfigurationSource.cs** - Source abstraction
- **IConfigurationProvider.cs** - Provider abstraction
- **IChangeToken.cs** - Change notification mechanism

### ✅ Implementations

- **ConfigurationRoot.cs** - Main configuration root with:
  - Hierarchical key-value access
  - Provider precedence (reverse order iteration)
  - Reload support via `Reload()` method
  - Change token support
- **ConfigurationSection.cs** - Section implementation with:
  - Path-aware key resolution (`section["Key"]` → `root["Section:Key"]`)
  - Child enumeration via `GetChildren()`
  - Recursive section access
- **ConfigurationBuilder.cs** - Builder implementation:
  - Source collection and management
  - Provider building from sources
  - Configuration root construction
- **ConfigurationPath.cs** - Path manipulation utilities:
  - `Combine()` - Combine path segments
  - `GetSectionKey()` - Extract section key from path
  - `GetParentPath()` - Get parent path
- **ConfigurationReloadToken.cs** - Change token implementation:
  - `OnReload()` - Trigger change callbacks
  - Callback registration and invocation

### ✅ Configuration Providers

- **JsonConfigurationProvider.cs** - JSON file provider:
  - Reads JSON files and flattens to key-value pairs
  - Handles nested objects (e.g., `{"A": {"B": "value"}}` → `"A:B": "value"`)
  - Supports arrays (e.g., `{"Items": [1, 2]}` → `"Items:0": "1"`, `"Items:1": "2"`)
  - Handles `reloadOnChange` option (future: file watching)
- **EnvironmentVariablesConfigurationProvider.cs** - Environment variable provider:
  - Reads from `Environment.GetEnvironmentVariables()`
  - Converts double underscores to colons (`A__B__C` → `A:B:C`)
  - Supports prefix filtering (optional)
  - Case-insensitive key matching

### ✅ Extension Methods

- **ConfigurationBuilderExtensions.cs** - Builder extensions:
  - `AddJsonFile(string path, bool optional, bool reloadOnChange)` - Add JSON source
  - `AddEnvironmentVariables()` - Add environment variable source
- **ConfigurationExtensions.cs** - Configuration extensions:
  - `GetValue<T>(string key)` - Get typed value
  - `GetValue<T>(string key, T defaultValue)` - Get typed value with default
  - `Bind<T>(T instance)` - Bind configuration to object
  - `BindInstance()` - Internal recursive binding logic

### Key Features Implemented

- **Hierarchical Keys**: Full support for colon-separated keys (`"A:B:C"`)
- **Section Navigation**: `GetSection()` and `GetChildren()` for hierarchical access
- **Source Precedence**: Later sources correctly override earlier ones
- **POCO Binding**: Reflection-based binding with type conversion
- **Nested Binding**: Recursive binding for complex object hierarchies
- **Change Tokens**: `IChangeToken` mechanism for reload notifications

## Current Usage Analysis

From the baseline application, we need to support:

1. **Configuration Access**
   ```csharp
   var connectionString = configuration.GetConnectionString("DefaultConnection");
   var value = configuration["MySetting"];
   ```

2. **Section Access**
   ```csharp
   var section = configuration.GetSection("ConnectionStrings");
   var value = section["DefaultConnection"];
   ```

3. **POCO Binding**
   ```csharp
   var options = new MyOptions();
   configuration.GetSection("MyOptions").Bind(options);
   ```

4. **Multiple Sources**
   ```csharp
   builder.AddJsonFile("appsettings.json");
   builder.AddJsonFile("appsettings.Development.json", optional: true);
   builder.AddEnvironmentVariables();
   ```

## Testing Strategy

### Unit Tests

1. **Configuration Builder Tests**
   - Add sources and build configuration
   - Verify source order affects precedence
   - Test builder properties

2. **Configuration Root Tests**
   - Get values from multiple providers
   - Verify precedence (later sources override earlier)
   - Test reload functionality
   - Test change tokens

3. **Configuration Section Tests**
   - Get section and access values
   - Navigate nested sections
   - Get children sections
   - Test path resolution

4. **JSON Provider Tests**
   - Load JSON files
   - Flatten nested objects
   - Handle arrays
   - Handle missing files (optional vs required)

5. **Environment Variables Provider Tests**
   - Load environment variables
   - Convert double underscores to colons
   - Test prefix filtering
   - Test case-insensitive matching

6. **Configuration Extensions Tests**
   - `GetValue<T>()` with type conversion
   - `Bind<T>()` for simple objects
   - `Bind<T>()` for nested objects
   - Type conversion edge cases

### Integration Tests

1. **Real-world Scenarios**
   - Build configuration as in `Program.cs`
   - Access configuration in controllers
   - Bind configuration to options objects

## Migration Status

Phase 2 has been successfully integrated into `MiniCore.Web`:

- ✅ Custom configuration framework wired into ASP.NET Core via `ConfigurationAdapter`
- ✅ `ConfigurationFactory` builds configuration from JSON and environment variables
- ✅ All existing functionality works with custom configuration
- ✅ Comprehensive test coverage (unit tests)
- ⚠️ Temporary adapter code (`ConfigurationAdapter.cs`) remains for ASP.NET Core compatibility
  - Will be removed in Phase 4 when we implement our own HostBuilder
  - See integration notes below

## Integration Details

### ConfigurationFactory.cs

A static factory class that builds the custom `IConfigurationRoot`:

```csharp
public static IConfigurationRoot CreateConfiguration(string basePath, string? environmentName = null)
{
    var builder = new ConfigurationBuilder();
    
    // Add appsettings.json
    builder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
    
    // Add environment-specific appsettings
    if (!string.IsNullOrEmpty(environmentName))
    {
        builder.AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: false);
    }
    
    // Add environment variables (highest precedence)
    builder.AddEnvironmentVariables();
    
    return builder.Build();
}
```

### ConfigurationAdapter.cs

Bridges our custom configuration with Microsoft's `IConfiguration` interface:

- **ConfigurationAdapter**: Adapts `IConfigurationRoot` → `Microsoft.Extensions.Configuration.IConfiguration`
- **ConfigurationSectionAdapter**: Adapts `IConfigurationSection` → `Microsoft.Extensions.Configuration.IConfigurationSection`
- **ChangeTokenAdapter**: Adapts `IChangeToken` → `Microsoft.Extensions.Primitives.IChangeToken`

This allows ASP.NET Core components (like `GetConnectionString()` extension methods) to work with our custom configuration.

## Success Criteria

- ✅ All core interfaces implemented and match Microsoft's API surface
- ✅ JSON file provider works correctly
- ✅ Environment variable provider works correctly
- ✅ Source precedence works (later sources override earlier)
- ✅ Configuration sections work correctly
- ✅ POCO binding works for simple and nested objects
- ✅ Unit tests for configuration framework pass (100%)
- ✅ No breaking changes to application code
- ✅ Configuration can be injected into controllers and services

## Known Limitations

### File Watching (reloadOnChange)

**Status:** `reloadOnChange` parameter is accepted but not yet implemented

**Current Behavior:** Configuration is loaded once at startup. Changes to JSON files are not automatically detected.

**Future Enhancement:** Implement `FileSystemWatcher` to monitor JSON files and trigger `Reload()` when files change. This requires:
- File system watching in `JsonConfigurationProvider`
- Proper disposal of watchers
- Thread-safe reload handling

### Configuration Reload Trigger

**Status:** `Reload()` method exists but is not automatically triggered

**Current Behavior:** Configuration reload must be manually triggered via `IConfigurationRoot.Reload()`.

**Real-world Usage:** In production .NET apps, configuration reload is typically triggered by:
- File system watchers (for JSON files)
- External configuration services (Azure App Configuration, etc.)
- Manual reload endpoints (for admin operations)

**Future Enhancement:** Implement automatic reload when `reloadOnChange: true` is specified.

### GetChildKeys Behavior

**Status:** `GetChildKeys()` returns only immediate children (one level deep)

**Current Behavior:** When getting children for `"A"`, keys like `"A:B:C"` are returned as a single child `"B:C"`, not expanded recursively.

**Design Decision:** This matches Microsoft's behavior. Multi-segment keys are treated as single child sections, not expanded into individual segments.

## Key Implementation Details

### Path Resolution in Sections

When accessing `section["Key"]` on a `ConfigurationSection` with path `"MySection"`:
1. Section combines its path with the key: `"MySection:Key"`
2. Calls `_root["MySection:Key"]` to get the value
3. Root queries providers in reverse order

### POCO Binding Flow

When binding a configuration section to an object:

1. **Get Section**: `config.GetSection("MySection")` → `ConfigurationSection`
2. **Start Binding**: `section.Bind<MyClass>(instance)` → calls `BindInstance()`
3. **Reflection**: Iterate through all public properties
4. **Value Retrieval**: For each property, call `config["PropertyName"]`
5. **Type Conversion**: Convert string values to property types using `Convert.ChangeType()`
6. **Recursive Binding**: For complex types, recursively call `BindInstance()`
7. **Path Building**: Nested properties build paths like `"MySection:Nested:Key"`

### Environment Variable Key Transformation

Environment variables use double underscores as separators:
- `A__B__C` → Configuration key `"A:B:C"`
- This allows setting nested configuration via environment variables
- Example: `ConnectionStrings__DefaultConnection=...` → `"ConnectionStrings:DefaultConnection"`

### Provider Precedence

Providers are queried in **reverse order** (last added first):
- If JSON provider is added first, then environment variables
- Environment variables will override JSON values
- This matches Microsoft's behavior where later sources have higher precedence

## Next Steps

Phase 2 is complete. Next phases:

- **Phase 3**: Logging Framework (will use configuration for log settings)
- **Phase 4**: Host Abstraction (will use configuration and DI as composition root)
- **Phase 5+**: Middleware, Routing, Server (will use configuration for settings)

## References

- [Microsoft.Extensions.Configuration Documentation](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration)
- [ASP.NET Core Configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration)

