# Phase 1: DI Implementation Checklist

## Project Setup

- [ ] Create `MiniCore.Framework` project
- [ ] Add project to solution (`MiniCore.sln`)
- [ ] Set target framework (net10.0 to match baseline)
- [ ] Create `DI` folder structure
- [ ] Add project reference from `MiniCore.Web` to `MiniCore.Framework`

## Core Types

- [ ] Implement `ServiceLifetime` enum (Singleton, Scoped, Transient)
- [ ] Implement `ServiceDescriptor` class
  - [ ] Service type, implementation type, instance, factory properties
  - [ ] Static factory methods for different registration patterns
  - [ ] Support for open generics

## Interfaces

- [ ] Implement `IServiceProvider` interface
  - [ ] `GetService(Type serviceType)` method
- [ ] Implement `IServiceCollection` interface
  - [ ] Extends `IList<ServiceDescriptor>`
- [ ] Implement `IServiceScope` interface
  - [ ] `ServiceProvider` property
  - [ ] `Dispose()` method
- [ ] Implement `IServiceScopeFactory` interface
  - [ ] `CreateScope()` method

## Core Implementations

- [ ] Implement `ServiceCollection` class
  - [ ] Inherits from `List<ServiceDescriptor>`
  - [ ] Implements `IServiceCollection`

- [ ] Implement `ServiceProvider` class
  - [ ] Implements `IServiceProvider` and `IServiceScopeFactory`
  - [ ] Singleton instance cache
  - [ ] Scoped instance cache (for root scope)
  - [ ] `GetService(Type)` method
  - [ ] Constructor injection resolver
  - [ ] Dependency resolution with cycle detection
  - [ ] Open generic resolution
  - [ ] Lifetime management (Transient, Scoped, Singleton)
  - [ ] Disposal support

- [ ] Implement `ServiceScope` class
  - [ ] Implements `IServiceScope`
  - [ ] Own scoped instance cache
  - [ ] Dispose scoped instances on disposal
  - [ ] Delegate singleton resolution to root provider

- [ ] Implement `ServiceProviderOptions` class
  - [ ] `ValidateScopes` property
  - [ ] `ValidateOnBuild` property

## Extension Methods

- [ ] Implement `ServiceCollectionExtensions`
  - [ ] `AddSingleton<TService, TImplementation>()`
  - [ ] `AddSingleton<TService>(TService instance)`
  - [ ] `AddSingleton<TService>(Func<IServiceProvider, TService> factory)`
  - [ ] `AddScoped<TService, TImplementation>()`
  - [ ] `AddScoped<TService>(Func<IServiceProvider, TService> factory)`
  - [ ] `AddTransient<TService, TImplementation>()`
  - [ ] `AddTransient<TService>(Func<IServiceProvider, TService> factory)`
  - [ ] Open generic variants (`AddSingleton(Type, Type)`, etc.)
  - [ ] `BuildServiceProvider()` methods

- [ ] Implement `ServiceProviderExtensions`
  - [ ] `GetRequiredService<T>()`
  - [ ] `GetService<T>()`
  - [ ] `GetRequiredService(IServiceProvider, Type)`

## Key Algorithms

- [ ] Constructor selection algorithm
  - [ ] Find all public constructors
  - [ ] Score by number of resolvable parameters
  - [ ] Select constructor with most resolvable parameters
  - [ ] Handle single constructor case
  - [ ] Error if no resolvable constructor

- [ ] Dependency resolution algorithm
  - [ ] Check if service is registered
  - [ ] Handle open generics
  - [ ] Check singleton cache
  - [ ] Check scoped cache (if in scope)
  - [ ] Create new instance (transient or new scoped)
  - [ ] Resolve constructor dependencies recursively

- [ ] Circular dependency detection
  - [ ] Track resolution stack
  - [ ] Detect cycles
  - [ ] Throw meaningful error message

- [ ] Open generic resolution
  - [ ] Extract generic type arguments
  - [ ] Find matching open generic registration
  - [ ] Construct closed generic implementation type
  - [ ] Register and resolve

## Testing

### Unit Tests

- [ ] Service registration tests
  - [ ] Register simple type
  - [ ] Register with instance
  - [ ] Register with factory
  - [ ] Register open generics

- [ ] Lifetime tests
  - [ ] Transient: different instances
  - [ ] Singleton: same instance
  - [ ] Scoped: same in scope, different across scopes

- [ ] Constructor injection tests
  - [ ] Single dependency
  - [ ] Multiple dependencies
  - [ ] Deep dependency chain
  - [ ] Optional dependencies (if supported)

- [ ] Error handling tests
  - [ ] Missing service throws exception
  - [ ] Circular dependency detection
  - [ ] No valid constructor error
  - [ ] Disposed provider throws exception

- [ ] Open generic tests
  - [ ] Register `ILogger<>` → `Logger<>`
  - [ ] Resolve `ILogger<MyClass>` → `Logger<MyClass>`
  - [ ] Multiple generic parameters

- [ ] Scope tests
  - [ ] Create scope and resolve scoped services
  - [ ] Dispose scope and verify cleanup
  - [ ] Scoped services disposed correctly

### Integration Tests

- [ ] Real-world registration pattern (as in Program.cs)
- [ ] Resolve controller with all dependencies
- [ ] Use scoped service in background service
- [ ] Verify all existing tests pass

## Migration

- [ ] Update `MiniCore.Web` to use `MiniCore.Framework.DependencyInjection`
- [ ] Remove `Microsoft.Extensions.DependencyInjection` package reference
- [ ] Verify application runs correctly
- [ ] Verify all tests pass
- [ ] Compare behavior with reference implementation

## Documentation

- [ ] Code comments for public APIs
- [ ] XML documentation comments
- [ ] Usage examples
- [ ] Migration guide

## Validation

- [ ] All tests pass
- [ ] No breaking changes to application code
- [ ] Performance is acceptable
- [ ] Memory leaks checked (scoped services disposed)
- [ ] Thread safety verified (if applicable)

## Phase Completion

- [ ] All checklist items completed
- [ ] Code reviewed
- [ ] Documentation updated
- [ ] Ready for Phase 2

