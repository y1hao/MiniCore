# Options Pattern Explanation

## What is the Options Pattern?

The **Options pattern** is a .NET Core design pattern that provides a strongly-typed way to access configuration settings. Instead of reading configuration values as strings and parsing them manually, you define classes that represent your configuration and bind them to configuration sections.

## Why It Exists

**Problem:** Configuration is typically stored as key-value pairs (JSON, environment variables, etc.), but applications need strongly-typed objects.

**Solution:** The Options pattern bridges this gap by:
1. Defining strongly-typed classes for configuration
2. Binding configuration sections to these classes
3. Providing these objects via Dependency Injection

## How It Works

### 1. Define a Configuration Class

```csharp
public class ConsoleLoggerOptions
{
    public Dictionary<string, ConsoleFormatter> Formatters { get; set; }
    public string DefaultFormatter { get; set; } = "simple";
    // ... other properties
}
```

### 2. Register Options in DI

```csharp
// In Program.cs or Startup.cs
builder.Services.Configure<ConsoleLoggerOptions>(options => {
    options.Formatters = new Dictionary<string, ConsoleFormatter> {
        { "simple", new SimpleConsoleFormatter() },
        { "json", new JsonConsoleFormatter() }
    };
    options.DefaultFormatter = "simple";
});
```

Or bind from configuration:

```csharp
builder.Services.Configure<ConsoleLoggerOptions>(
    builder.Configuration.GetSection("Logging:Console")
);
```

### 3. Inject Options into Services

```csharp
public class ConsoleLoggerProvider
{
    private readonly ConsoleLoggerOptions _options;
    
    public ConsoleLoggerProvider(IOptionsMonitor<ConsoleLoggerOptions> optionsMonitor)
    {
        // IOptionsMonitor provides the current options value
        _options = optionsMonitor.CurrentValue;
        
        // IOptionsMonitor can also notify when options change
        optionsMonitor.OnChange(newOptions => {
            // Reload options when configuration changes
            ReloadLoggerOptions(newOptions);
        });
    }
    
    private void ReloadLoggerOptions(ConsoleLoggerOptions options)
    {
        // Access the formatters dictionary
        var formatter = options.Formatters[options.DefaultFormatter]; // ← This is where our error occurs!
        // ...
    }
}
```

## The Three Options Interfaces

.NET Core provides three interfaces for accessing options:

### `IOptions<T>`
- **Lifetime:** Singleton
- **Behavior:** Options are read once at startup and cached
- **Use case:** When options never change during application lifetime

```csharp
public class MyService
{
    public MyService(IOptions<MySettings> options)
    {
        var settings = options.Value; // Read once, cached forever
    }
}
```

### `IOptionsSnapshot<T>`
- **Lifetime:** Scoped (per request)
- **Behavior:** Options are recomputed for each scope/request
- **Use case:** When you need fresh options on each request (e.g., multi-tenant apps)

```csharp
public class MyService
{
    public MyService(IOptionsSnapshot<MySettings> options)
    {
        var settings = options.Value; // Fresh value for this request
    }
}
```

### `IOptionsMonitor<T>`
- **Lifetime:** Singleton
- **Behavior:** Options are cached but can notify when configuration changes
- **Use case:** When you need to react to configuration changes at runtime

```csharp
public class MyService
{
    public MyService(IOptionsMonitor<MySettings> optionsMonitor)
    {
        var settings = optionsMonitor.CurrentValue; // Current cached value
        
        // Subscribe to changes
        optionsMonitor.OnChange(newSettings => {
            // React to configuration changes
        });
    }
}
```

## Why ConsoleLoggerProvider Uses IOptionsMonitor

`ConsoleLoggerProvider` uses `IOptionsMonitor<ConsoleLoggerOptions>` because:

1. **It needs to react to configuration changes** - If you change logging configuration, it should reload without restarting the app
2. **It needs access to formatters** - The options object contains a dictionary of formatters (like "simple", "json")
3. **It needs default values** - The options should have sensible defaults if not configured

## What's Happening in Our Error

### The Error
```
System.Collections.Generic.KeyNotFoundException : The given key 'simple' was not present in the dictionary.
at Microsoft.Extensions.Logging.Console.ConsoleLoggerProvider.ReloadLoggerOptions(ConsoleLoggerOptions options)
```

### Why It Happens

1. **ConsoleLoggerProvider is constructed** during app startup
2. **It requests `IOptionsMonitor<ConsoleLoggerOptions>`** from the DI container
3. **Our DI container tries to resolve it**, but:
   - We don't have `IOptionsMonitor<T>` registered
   - We don't have `ConsoleLoggerOptions` configured
   - We don't have the Options pattern infrastructure

4. **Even if we could resolve it**, the `ConsoleLoggerOptions` object would be:
   - Empty/uninitialized
   - Missing the `Formatters` dictionary
   - Missing the default "simple" formatter entry

5. **When `ReloadLoggerOptions` runs**, it tries to access:
   ```csharp
   var formatter = options.Formatters["simple"]; // ← KeyNotFoundException!
   ```

## What's Missing in Our Implementation

### Phase 1 (Current) - DI Container ✅
- ✅ We can resolve services
- ✅ We can inject dependencies
- ❌ We don't have Options pattern support

### Phase 2 (Configuration) - Will Add
- ✅ Configuration reading (JSON, environment variables)
- ✅ Configuration binding (`config.Bind<T>()`)
- ❌ Still no Options pattern DI integration

### Phase 3 (Logging) - Will Add Options Pattern
- ✅ `IOptions<T>`, `IOptionsSnapshot<T>`, `IOptionsMonitor<T>` interfaces
- ✅ Options registration (`Configure<TOptions>()`)
- ✅ Options factory registration in DI
- ✅ Configuration-to-options binding
- ✅ Default values support

## How Microsoft's Options Framework Works

Microsoft's `Microsoft.Extensions.Options` package provides:

### 1. Options Registration
```csharp
// This registers a factory that creates IOptionsMonitor<T>
services.AddOptions<ConsoleLoggerOptions>()
    .Configure(options => {
        // Set defaults
        options.Formatters = new Dictionary<string, ConsoleFormatter> {
            { "simple", new SimpleConsoleFormatter() }
        };
    })
    .Bind(configuration.GetSection("Logging:Console"));
```

### 2. Options Factory
When you request `IOptionsMonitor<ConsoleLoggerOptions>`, the DI container:
1. Finds the registered `IOptionsFactory<ConsoleLoggerOptions>`
2. Calls the factory to create an `OptionsMonitor<ConsoleLoggerOptions>`
3. The monitor:
   - Reads configuration
   - Binds it to `ConsoleLoggerOptions`
   - Merges with defaults
   - Caches the result
   - Sets up change notifications

### 3. Options Resolution
```csharp
// In ServiceProvider
public object GetService(Type serviceType)
{
    if (serviceType.IsGenericType)
    {
        var genericDef = serviceType.GetGenericTypeDefinition();
        
        if (genericDef == typeof(IOptionsMonitor<>))
        {
            var optionsType = serviceType.GetGenericArguments()[0];
            // Find registered options factory
            // Create OptionsMonitor instance
            // Return it
        }
    }
    // ...
}
```

## What We Need to Implement

### In Phase 3 (Logging Framework)

1. **Options Interfaces**
   ```csharp
   public interface IOptions<out T> where T : class
   {
       T Value { get; }
   }
   
   public interface IOptionsMonitor<T> where T : class
   {
       T CurrentValue { get; }
       IDisposable OnChange(Action<T> listener);
   }
   ```

2. **Options Registration**
   ```csharp
   public static IServiceCollection Configure<TOptions>(
       this IServiceCollection services,
       Action<TOptions> configure) where TOptions : class
   {
       // Register factory that creates configured options
       services.AddSingleton<IOptionsFactory<TOptions>>(sp => 
           new ConfigureOptionsFactory<TOptions>(configure));
       services.AddSingleton<IOptionsMonitor<TOptions>>(sp => 
           new OptionsMonitor<TOptions>(...));
       return services;
   }
   ```

3. **Options Monitor Implementation**
   ```csharp
   public class OptionsMonitor<TOptions> : IOptionsMonitor<TOptions>
   {
       private TOptions _currentValue;
       
       public TOptions CurrentValue => _currentValue ??= CreateOptions();
       
       private TOptions CreateOptions()
       {
           // Read from configuration
           // Bind to TOptions
           // Apply defaults
           // Return configured instance
       }
   }
   ```

## Summary

**The Options Pattern:**
- Provides strongly-typed configuration access
- Bridges configuration (key-value) to objects (classes)
- Supports runtime configuration changes
- Integrates with Dependency Injection

**Why Our Tests Fail:**
- `ConsoleLoggerProvider` needs `IOptionsMonitor<ConsoleLoggerOptions>`
- We don't have Options pattern infrastructure yet
- Even if we could resolve it, `ConsoleLoggerOptions` wouldn't be configured
- The options object would be missing required formatters

**When Tests Will Pass:**
- After Phase 3 (Logging Framework) implements the Options pattern
- Options will be properly registered and configured
- `ConsoleLoggerOptions` will have default formatters
- `IOptionsMonitor<T>` will be resolvable from our DI container

## References

- [.NET Options Pattern Documentation](https://learn.microsoft.com/en-us/dotnet/core/extensions/options)
- [ASP.NET Core Configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [Microsoft.Extensions.Options Source Code](https://github.com/dotnet/runtime/tree/main/src/libraries/Microsoft.Extensions.Options)

