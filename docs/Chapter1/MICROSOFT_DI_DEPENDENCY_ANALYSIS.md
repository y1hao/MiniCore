# Microsoft Dependency Injection Dependency Analysis

## Current State

We have successfully implemented our custom Dependency Injection framework (Phase 1), but we still have a dependency on `Microsoft.Extensions.DependencyInjection`. This document analyzes why this dependency exists and when it can be removed.

## Why We Still Need Microsoft's DI

### 1. **ASP.NET Core's WebApplication Builder**

The root cause is `WebApplication.CreateBuilder()` in `Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);
```

**What happens internally:**
- `WebApplication.CreateBuilder()` creates a `WebApplicationBuilder` instance
- This builder internally creates a `Microsoft.Extensions.DependencyInjection.IServiceCollection`
- All ASP.NET Core extension methods (`AddControllers()`, `AddDbContext()`, etc.) register services into this Microsoft collection
- The builder uses Microsoft's DI as the default service provider

### 2. **Service Registration Phase**

All service registrations use Microsoft's `IServiceCollection`:

```csharp
builder.Services.AddDbContext<AppDbContext>(...);  // Microsoft's IServiceCollection
builder.Services.AddControllers();                  // Microsoft's IServiceCollection
builder.Services.AddRazorPages();                  // Microsoft's IServiceCollection
```

**Why:** These are ASP.NET Core extension methods that expect `Microsoft.Extensions.DependencyInjection.IServiceCollection`.

### 3. **ServiceProviderFactory Bridge**

We use `IServiceProviderFactory<T>` to bridge Microsoft's DI with ours:

```csharp
builder.Host.UseServiceProviderFactory(new ServiceProviderFactory());
```

**What this does:**
- Allows ASP.NET Core to use our custom DI container
- But requires converting Microsoft's service descriptors to ours
- Still depends on Microsoft's interfaces (`IServiceProviderFactory`, `IServiceCollection`, `IServiceScope`)

### 4. **Dependencies in ServiceProviderFactory**

Our `ServiceProviderFactory` depends on Microsoft's types:

| Microsoft Type | Used For | Our Equivalent |
|----------------|----------|----------------|
| `IServiceProviderFactory<T>` | Interface to plug in custom DI | None (we implement it) |
| `IServiceCollection` | Input from ASP.NET Core builder | `MiniCore.Framework.DependencyInjection.ServiceCollection` |
| `ServiceDescriptor` | Service registration metadata | `MiniCore.Framework.DependencyInjection.ServiceDescriptor` |
| `ServiceLifetime` | Lifetime enum | `MiniCore.Framework.DependencyInjection.ServiceLifetime` |
| `IServiceScopeFactory` | Factory interface | `MiniCore.Framework.DependencyInjection.IServiceScopeFactory` |
| `IServiceScope` | Scope interface | `MiniCore.Framework.DependencyInjection.IServiceScope` |

## Current Architecture

```
┌─────────────────────────────────────────────────────────┐
│  ASP.NET Core WebApplication.CreateBuilder()            │
│  └─► Creates Microsoft.Extensions.DependencyInjection  │
│      └─► IServiceCollection (Microsoft)                │
└─────────────────────────────────────────────────────────┘
                    │
                    │ builder.Services.AddXxx()
                    ▼
┌─────────────────────────────────────────────────────────┐
│  ServiceProviderFactory                                 │
│  └─► Converts Microsoft descriptors → Our descriptors  │
│  └─► Builds our ServiceProvider                        │
└─────────────────────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────────────────────┐
│  Our Custom DI Container                                │
│  └─► MiniCore.Framework.DependencyInjection            │
└─────────────────────────────────────────────────────────┘
```

## When Can We Remove Microsoft's DI?

### Phase 4: Host Abstraction ✅ **This is when we can remove it**

**What Phase 4 will do:**
- Create our own `MiniHostBuilder` that uses our DI from the start
- Replace `WebApplication.CreateBuilder()` with our own builder
- Register services directly into our `IServiceCollection`
- No conversion needed - everything uses our DI natively

**Expected changes:**

**Before (Current):**
```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Host.UseServiceProviderFactory(new ServiceProviderFactory());
builder.Services.AddControllers();  // Microsoft's IServiceCollection
```

**After (Phase 4):**
```csharp
var builder = new MiniHostBuilder();
builder.ConfigureServices(services => {
    services.AddControllers();  // Our IServiceCollection
});
var host = builder.Build();
```

### Phase 5-7: Middleware, Routing, Server

These phases will also use our DI directly:
- **Phase 5 (Middleware)**: Middleware will be resolved from our DI container
- **Phase 6 (Routing)**: Route handlers will use our DI
- **Phase 7 (Server)**: Server will use our DI for service resolution

## Migration Path

### Step 1: Phase 4 - Host Abstraction

1. **Create `MiniHostBuilder`**:
   ```csharp
   public class MiniHostBuilder
   {
       private readonly ServiceCollection _services = new();
       
       public MiniHostBuilder ConfigureServices(Action<IServiceCollection> configure)
       {
           configure(_services);
           return this;
       }
       
       public MiniHost Build()
       {
           var serviceProvider = _services.BuildServiceProvider();
           return new MiniHost(serviceProvider, ...);
       }
   }
   ```

2. **Replace `Program.cs`**:
   ```csharp
   // OLD: var builder = WebApplication.CreateBuilder(args);
   var builder = new MiniHostBuilder();
   
   builder.ConfigureServices(services => {
       services.AddControllers();
       services.AddDbContext<AppDbContext>(...);
       // All registrations go directly into our IServiceCollection
   });
   
   var host = builder.Build();
   await host.RunAsync();
   ```

3. **Remove `ServiceProviderFactory`**:
   - No longer needed since we're not bridging Microsoft's DI
   - Can delete `ServiceProviderFactory.cs` entirely

### Step 2: Remove Package Reference

Once Phase 4 is complete, we can remove:
```xml
<!-- No longer needed -->
<PackageReference Include="Microsoft.Extensions.DependencyInjection" />
```

**Note:** We might still need it transitively through other ASP.NET Core packages until we replace those too.

## Current Dependencies Breakdown

### Direct Dependencies (Can remove in Phase 4)

| File | Microsoft DI Usage | Can Remove When |
|------|-------------------|-----------------|
| `ServiceProviderFactory.cs` | Entire file bridges Microsoft → Our DI | Phase 4 |
| `Program.cs` | `WebApplication.CreateBuilder()` | Phase 4 |

### Indirect Dependencies (Will remove in later phases)

| Component | Microsoft DI Usage | Can Remove When |
|-----------|-------------------|-----------------|
| ASP.NET Core MVC | Uses Microsoft DI internally | Phase 5-6 (when we replace routing/middleware) |
| Entity Framework Core | Uses Microsoft DI for DbContext | Phase 8 (when we replace ORM) |
| Razor Pages | Uses Microsoft DI for view rendering | Phase 9 (when we replace templating) |

## Summary

**Current Status:**
- ✅ We have a fully functional custom DI container
- ✅ It's being used for service resolution (via `ServiceProviderFactory`)
- ⚠️ We still depend on Microsoft's DI for service registration

**Why:**
- ASP.NET Core's `WebApplication.CreateBuilder()` creates Microsoft's `IServiceCollection`
- All ASP.NET Core extension methods register into Microsoft's collection
- We bridge Microsoft's DI → Our DI via `ServiceProviderFactory`

**When we can remove it:**
- **Phase 4 (Host Abstraction)**: Remove direct dependency on Microsoft's DI interfaces
- **Phase 5-7**: Remove indirect dependencies as we replace middleware/routing/server
- **Phase 8-9**: Remove remaining dependencies as we replace ORM and templating

**Next Steps:**
1. Complete Phase 2 (Configuration) and Phase 3 (Logging)
2. Implement Phase 4 (Host Abstraction) - this is the key milestone
3. Replace `WebApplication.CreateBuilder()` with `MiniHostBuilder`
4. Delete `ServiceProviderFactory.cs`
5. Remove `Microsoft.Extensions.DependencyInjection` package reference

## Conclusion

The Microsoft DI dependency is a **temporary bridge** needed because we're still using ASP.NET Core's builder infrastructure. Once we implement our own `HostBuilder` in Phase 4, we can remove this dependency entirely and use our custom DI container natively throughout the application.

