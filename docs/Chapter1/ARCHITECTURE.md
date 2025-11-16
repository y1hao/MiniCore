# Phase 1: DI Architecture Overview

## System Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Application Layer                        │
│  (Controllers, Services, Background Services)               │
│                                                             │
│  Uses: IServiceProvider, IServiceScope                     │
└─────────────────────────────────────────────────────────────┘
                            │
                            │ depends on
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                  DI Framework Layer                          │
│                                                             │
│  ┌──────────────────┐  ┌──────────────────┐                │
│  │ IServiceProvider │  │ IServiceScope    │                │
│  │                  │  │                  │                │
│  │ GetService()    │  │ ServiceProvider  │                │
│  └──────────────────┘  │ Dispose()        │                │
│                        └──────────────────┘                │
│                                                             │
│  ┌──────────────────┐  ┌──────────────────┐                │
│  │ IServiceCollection│ │IServiceScopeFactory│              │
│  │                  │  │                  │                │
│  │ AddSingleton()  │  │ CreateScope()    │                │
│  │ AddScoped()     │  │                  │                │
│  │ AddTransient()  │  └──────────────────┘                │
│  └──────────────────┘                                      │
└─────────────────────────────────────────────────────────────┘
                            │
                            │ implements
                            ▼
┌─────────────────────────────────────────────────────────────┐
│              DI Implementation Layer                         │
│                                                             │
│  ┌──────────────────┐  ┌──────────────────┐                │
│  │ ServiceProvider  │  │ ServiceScope     │                │
│  │                  │  │                  │                │
│  │ - singletons     │  │ - scopedInstances│                │
│  │ - scopedInstances│  │ - serviceProvider│                │
│  │ - services       │  │                  │                │
│  │                  │  │ Dispose()        │                │
│  │ GetService()     │  └──────────────────┘                │
│  │ CreateScope()    │                                      │
│  └──────────────────┘                                      │
│                                                             │
│  ┌──────────────────┐  ┌──────────────────┐                │
│  │ ServiceCollection│ │ ServiceDescriptor │               │
│  │                  │  │                  │                │
│  │ - List<Service   │  │ - ServiceType    │                │
│  │   Descriptor>    │  │ - Implementation │                │
│  │                  │  │ - Lifetime       │                │
│  │ Add()            │  │ - Factory        │                │
│  └──────────────────┘  └──────────────────┘                │
└─────────────────────────────────────────────────────────────┘
```

## Service Resolution Flow

```
GetService(Type serviceType)
    │
    ├─► Is registered?
    │   │
    │   ├─► NO → Return null
    │   │
    │   └─► YES
    │       │
    │       ├─► Is open generic?
    │       │   │
    │       │   └─► Resolve open generic
    │       │
    │       └─► Get descriptor
    │           │
    │           ├─► Lifetime?
    │           │   │
    │           │   ├─► Singleton
    │           │   │   ├─► Check cache
    │           │   │   └─► Create if not exists
    │           │   │
    │           │   ├─► Scoped
    │           │   │   ├─► Check scope cache
    │           │   │   └─► Create if not exists
    │           │   │
    │           │   └─► Transient
    │           │       └─► Always create new
    │           │
    │           └─► Create instance
    │               │
    │               ├─► Has factory?
    │               │   └─► Call factory
    │               │
    │               ├─► Has instance?
    │               │   └─► Return instance
    │               │
    │               └─► Has implementation type?
    │                   │
    │                   ├─► Find constructor
    │                   ├─► Resolve dependencies
    │                   │   └─► (Recursive GetService calls)
    │                   └─► Create instance
```

## Lifetime Management

### Singleton
```
┌─────────────────────────────────────┐
│     ServiceProvider (Root)          │
│                                     │
│  ┌───────────────────────────────┐  │
│  │ Singleton Cache               │  │
│  │  Type → Instance              │  │
│  │  ─────────────────            │  │
│  │  IConfig → ConfigInstance     │  │
│  │  Logger → LoggerInstance      │  │
│  └───────────────────────────────┘  │
│                                     │
│  All scopes share this cache        │
└─────────────────────────────────────┘
```

### Scoped
```
┌─────────────────────────────────────┐
│     ServiceProvider (Root)          │
│                                     │
│  ┌───────────────────────────────┐  │
│  │ Scope 1                       │  │
│  │  ┌─────────────────────────┐  │  │
│  │  │ Scoped Cache            │  │  │
│  │  │ DbContext → Instance1   │  │  │
│  │  └─────────────────────────┘  │  │
│  └───────────────────────────────┘  │
│                                     │
│  ┌───────────────────────────────┐  │
│  │ Scope 2                       │  │
│  │  ┌─────────────────────────┐  │  │
│  │  │ Scoped Cache            │  │  │
│  │  │ DbContext → Instance2   │  │  │
│  │  └─────────────────────────┘  │  │
│  └───────────────────────────────┘  │
│                                     │
│  Each scope has its own cache       │
└─────────────────────────────────────┘
```

### Transient
```
┌─────────────────────────────────────┐
│     ServiceProvider                 │
│                                     │
│  GetService(TransientService)       │
│    │                                │
│    ├─► Create new instance 1       │
│    │                                │
│  GetService(TransientService)       │
│    │                                │
│    └─► Create new instance 2       │
│                                     │
│  No caching - always new instance   │
└─────────────────────────────────────┘
```

## Constructor Injection Flow

```
CreateInstance(Type type)
    │
    ├─► Get constructors
    │
    ├─► Score constructors
    │   │
    │   └─► Can resolve all parameters?
    │       │
    │       ├─► YES → Score = parameter count
    │       │
    │       └─► NO → Score = 0 (skip)
    │
    ├─► Select best constructor
    │   │
    │   └─► (Highest score, all parameters resolvable)
    │
    ├─► Resolve parameters
    │   │
    │   ├─► For each parameter:
    │   │   │
    │   │   ├─► Check circular dependency
    │   │   │   │
    │   │   │   └─► Add to resolution stack
    │   │   │
    │   │   └─► GetService(parameterType)
    │   │       │
    │   │       └─► (Recursive resolution)
    │   │
    │   └─► Remove from resolution stack
    │
    └─► Create instance with resolved parameters
```

## Open Generic Resolution

```
Register: ILogger<> → Logger<>
    │
    └─► Stored as open generic descriptor

Resolve: ILogger<MyClass>
    │
    ├─► Extract generic arguments: [MyClass]
    │
    ├─► Find open generic registration: ILogger<>
    │
    ├─► Construct closed generic: Logger<MyClass>
    │
    ├─► Create descriptor for closed generic
    │
    └─► Resolve as normal service
```

## Component Relationships

```
ServiceCollection
    │
    │ contains
    │
    ▼
ServiceDescriptor[]
    │
    │ describes
    │
    ▼
ServiceProvider
    │
    │ uses to
    │
    ├─► Resolve services
    │
    └─► Create scopes
        │
        │ creates
        │
        ▼
    ServiceScope
        │
        │ manages
        │
        └─► Scoped instances
```

## Error Scenarios

### Circular Dependency
```
A depends on B
B depends on C
C depends on A

Resolution Stack:
  A → B → C → A  ❌ CIRCULAR!
```

### Missing Service
```
Resolve: IService
    │
    └─► Not registered ❌
        │
        └─► Throw: "Unable to resolve service for type 'IService'"
```

### No Valid Constructor
```
Type: MyClass
Constructors:
  - MyClass(IDependency1, IDependency2)  ❌ IDependency2 not registered
  - MyClass(IDependency1)  ✅ All dependencies registered

Select: MyClass(IDependency1)
```

## Thread Safety Considerations

```
┌─────────────────────────────────────┐
│     Thread Safety Model             │
│                                     │
│  Singleton Creation:                │
│  - Double-check locking             │
│  - Thread-safe cache                │
│                                     │
│  Scoped Instances:                  │
│  - Per-scope cache (not shared)     │
│  - Thread-local resolution stack    │
│                                     │
│  Transient:                         │
│  - No shared state                  │
│  - Thread-safe                      │
└─────────────────────────────────────┘
```

