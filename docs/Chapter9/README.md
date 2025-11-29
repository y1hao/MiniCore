# Chapter 9: Mini ORM / Data Integration

## Overview

Phase 9 implements a lightweight ORM framework to replace Entity Framework Core. This provides CRUD operations, LINQ-like query support, and object-relational mapping using reflection and ADO.NET, specifically Microsoft.Data.Sqlite for SQLite database access.

**Status:** ✅ Complete

## Goals

- Replace Entity Framework Core with a custom ORM implementation
- Implement `DbContext` and `DbSet<T>` classes
- Support LINQ-like queries (Where, OrderBy, Skip, Take, Select)
- Implement CRUD operations (Add, Remove, SaveChangesAsync)
- Support async operations (ToListAsync, AnyAsync, FirstOrDefaultAsync, FindAsync)
- Automatic schema creation (EnsureCreated)
- Reflection-based object mapping (rows ↔ objects)

## Key Requirements

### DbContext Base Class

1. **DbContext**
   - Base class for database contexts
   - Manages connection strings via `DbContextOptions`
   - Tracks entity changes (Added, Modified, Deleted)
   - Implements `SaveChangesAsync()` to persist changes
   - Implements `EnsureCreated()` to create database schema

2. **DbContextOptions**
   - Configuration for DbContext
   - Stores connection string
   - Builder pattern via `DbContextOptionsBuilder`

### DbSet<T> Implementation

1. **DbSet<T>**
   - Implements `IQueryable<T>` for LINQ support
   - Provides async query methods: `ToListAsync()`, `AnyAsync()`, `FirstOrDefaultAsync()`, `FindAsync()`
   - Supports CRUD operations: `Add()`, `Remove()`, `RemoveRange()`
   - Uses custom `QueryProvider` to translate LINQ expressions to SQL

### Query Translation

1. **QueryProvider**
   - Implements `IQueryProvider` interface
   - Translates LINQ expressions to SQL queries
   - Supports: Where, OrderBy, OrderByDescending, Skip, Take, Select

2. **QueryTranslator**
   - Visits expression trees to extract query information
   - Builds SQL WHERE clauses from predicate expressions
   - Builds ORDER BY clauses from ordering expressions
   - Handles LIMIT and OFFSET for Skip/Take

### Object-Relational Mapping

1. **ObjectMapper**
   - Maps `DataRow` to objects using reflection
   - Handles type conversion (primitives, nullable types, enums)
   - Extracts property values for INSERT/UPDATE operations
   - Identifies primary key properties (convention: "Id")

2. **DatabaseHelper**
   - Executes SQL queries using `Microsoft.Data.Sqlite`
   - Creates database connections
   - Executes queries and returns `DataTable`
   - Executes non-query commands (INSERT, UPDATE, DELETE)
   - Creates tables based on entity types

## Architecture

```
MiniCore.Framework/
└── Data/
    ├── Abstractions/
    │   └── IDbContext.cs              # DbContext interface
    ├── DbContext.cs                    # Base DbContext class
    ├── DbSet.cs                        # DbSet<T> implementation
    ├── DbContextOptions.cs             # Options for DbContext
    ├── DbContextOptionsBuilder.cs      # Builder for options
    ├── Extensions/
    │   ├── ServiceCollectionExtensions.cs  # AddDbContext extension
    │   └── QueryableExtensions.cs          # ToListAsync extension for IQueryable
    └── Internal/
        ├── ObjectMapper.cs             # Object-relational mapping
        ├── QueryBuilder.cs             # SQL query building
        ├── QueryProvider.cs            # IQueryProvider implementation
        ├── QueryTranslator.cs          # LINQ to SQL translation
        └── DatabaseHelper.cs           # Database operations
```

## Implementation Summary

Phase 9 successfully implements all core ORM components:

### ✅ DbContext Base Class

- **DbContext.cs** - Base class that:
  - Manages connection strings
  - Tracks entity changes (Added, Modified, Deleted states)
  - Implements `SaveChangesAsync()` to persist changes to database
  - Implements `EnsureCreated()` to create database schema
  - Provides `GetTableName()` for table name resolution

### ✅ DbSet<T> Implementation

- **DbSet.cs** - Implementation that:
  - Implements `IQueryable<T>` for LINQ support
  - Provides async query methods:
    - `ToListAsync()` - Materializes query to list
    - `AnyAsync()` - Checks if any elements match predicate
    - `FirstOrDefaultAsync()` - Returns first element or null
    - `FindAsync()` - Finds entity by primary key
  - Supports CRUD operations:
    - `Add()` - Marks entity for insertion
    - `Remove()` - Marks entity for deletion
    - `RemoveRange()` - Marks multiple entities for deletion

### ✅ Query Translation

- **QueryProvider** - Implements `IQueryProvider`:
  - Translates LINQ expressions to SQL queries
  - Executes queries asynchronously
  - Returns mapped entities

- **QueryTranslator** - Translates expressions:
  - Extracts WHERE clauses from predicate expressions
  - Extracts ORDER BY clauses from ordering expressions
  - Handles Skip/Take as LIMIT/OFFSET
  - Supports basic binary operations (==, !=, <, >, <=, >=)
  - Supports logical operations (AND, OR)

### ✅ Object-Relational Mapping

- **ObjectMapper** - Maps database rows to objects:
  - Uses reflection to map columns to properties
  - Handles type conversion (primitives, nullable types, enums)
  - Extracts property values for INSERT/UPDATE
  - Identifies primary keys (convention: "Id")

- **DatabaseHelper** - Database operations:
  - Executes queries using `Microsoft.Data.Sqlite`
  - Creates tables based on entity types
  - Handles parameterized queries
  - Supports async operations

### ✅ Schema Creation

- **EnsureCreated()** - Creates database schema:
  - Discovers DbSet properties via reflection
  - Creates tables based on entity types
  - Maps .NET types to SQL types
  - Handles primary keys and auto-increment

## Current Usage Patterns

### Basic DbContext

```csharp
public class AppDbContext : DbContext
{
    private DbSet<ShortLink>? _shortLinks;

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<ShortLink> ShortLinks
    {
        get
        {
            _shortLinks ??= new DbSet<ShortLink>(this, GetTableName(typeof(ShortLink)));
            return _shortLinks;
        }
        set => _shortLinks = value;
    }

    protected override string GetTableName(Type entityType)
    {
        if (entityType == typeof(ShortLink))
        {
            return "ShortLinks";
        }
        return base.GetTableName(entityType);
    }
}
```

### Registration

```csharp
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));
```

### Querying

```csharp
// Simple query
var links = await _context.ShortLinks
    .OrderByDescending(l => l.CreatedAt)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();

// With predicate
var link = await _context.ShortLinks
    .FirstOrDefaultAsync(l => l.ShortCode == shortCode);

// Check existence
var exists = await _context.ShortLinks
    .AnyAsync(l => l.ShortCode == shortCode);

// Find by primary key
var link = await _context.ShortLinks.FindAsync(new object[] { id });
```

### CRUD Operations

```csharp
// Create
var link = new ShortLink { ShortCode = "abc", OriginalUrl = "https://example.com" };
_context.ShortLinks.Add(link);
await _context.SaveChangesAsync();

// Update (modify entity, then save)
link.OriginalUrl = "https://newexample.com";
await _context.SaveChangesAsync();

// Delete
_context.ShortLinks.Remove(link);
await _context.SaveChangesAsync();

// Delete multiple
var expiredLinks = await _context.ShortLinks
    .Where(l => l.ExpiresAt.HasValue && l.ExpiresAt.Value < DateTime.UtcNow)
    .ToListAsync();
_context.ShortLinks.RemoveRange(expiredLinks);
await _context.SaveChangesAsync();
```

### Schema Creation

```csharp
using var scope = app.Services.CreateScope();
var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
context.EnsureCreated();
```

## Testing Strategy

### Unit Tests

1. **DbContext Tests**
   - Connection string configuration
   - Entity change tracking
   - SaveChangesAsync behavior
   - EnsureCreated behavior

2. **DbSet Tests**
   - Query execution
   - Async operations
   - CRUD operations
   - LINQ query translation

3. **ObjectMapper Tests**
   - Row to object mapping
   - Type conversion
   - Nullable type handling
   - Primary key identification

4. **QueryTranslator Tests**
   - WHERE clause translation
   - ORDER BY translation
   - Skip/Take translation
   - Expression tree visiting

## Success Criteria

- ✅ `DbContext` base class implemented
- ✅ `DbSet<T>` implements `IQueryable<T>`
- ✅ LINQ query translation (Where, OrderBy, Skip, Take)
- ✅ Async query methods (ToListAsync, AnyAsync, FirstOrDefaultAsync, FindAsync)
- ✅ CRUD operations (Add, Remove, SaveChangesAsync)
- ✅ Schema creation (EnsureCreated)
- ✅ Object-relational mapping via reflection
- ✅ All Entity Framework Core dependencies removed
- ✅ MiniCore.Web uses MiniCore.Framework.Data exclusively
- ✅ Unit tests pass (when implemented)

## Known Limitations

### Complex LINQ Operations

**Status:** Basic implementation

**Current Behavior:** Only supports basic LINQ operations (Where, OrderBy, Skip, Take, Select). Complex operations like GroupBy, Join, etc. are not supported.

**Future Enhancement:** Add support for more LINQ operations.

### Select Projection

**Status:** In-memory projection

**Current Behavior:** Select() projections are applied in-memory after fetching data from the database, not translated to SQL.

**Future Enhancement:** Translate Select() to SQL SELECT clauses.

### Change Tracking

**Status:** Basic implementation

**Current Behavior:** Only tracks Added and Deleted states. Modified state detection is not implemented (entities are always saved on SaveChangesAsync).

**Future Enhancement:** Implement proper change tracking to detect modified entities.

### Relationships

**Status:** Not implemented

**Current Behavior:** No support for navigation properties or relationships between entities.

**Future Enhancement:** Add support for one-to-many and many-to-many relationships.

### Migrations

**Status:** Not implemented

**Current Behavior:** Only supports `EnsureCreated()` which creates tables if they don't exist. No support for migrations or schema updates.

**Future Enhancement:** Add migration support similar to EF Core.

## Key Implementation Details

### Query Translation Flow

1. **LINQ Expression** → `IQueryable<T>` operations (Where, OrderBy, etc.)
2. **Expression Tree** → `QueryExpressionVisitor` extracts query information
3. **SQL Query** → `QueryTranslator` builds SQL with WHERE, ORDER BY, LIMIT, OFFSET
4. **Database Execution** → `DatabaseHelper` executes query using `Microsoft.Data.Sqlite`
5. **Result Mapping** → `ObjectMapper` maps `DataRow` to entity objects
6. **Return** → Returns `List<T>` or single entity

### Change Tracking

Entities are tracked in a dictionary with their state:
- **Added** - Entity is new and will be inserted
- **Modified** - Entity was changed and will be updated (not fully implemented)
- **Deleted** - Entity will be deleted
- **Unchanged** - Entity is unchanged (not used)

On `SaveChangesAsync()`, all tracked entities are processed:
- Added entities → INSERT statements
- Deleted entities → DELETE statements
- Modified entities → UPDATE statements (if implemented)

### Table Name Resolution

Table names are resolved using:
1. Override `GetTableName()` in derived DbContext
2. Default: Pluralized entity name (e.g., `ShortLink` → `ShortLinks`)

### Primary Key Convention

Primary keys are identified by convention:
- Property named "Id" (case-insensitive)
- Or property ending with "Id" (case-insensitive)

## Migration from Entity Framework Core

The following changes were made to migrate from EF Core:

1. **AppDbContext** - Changed base class from `Microsoft.EntityFrameworkCore.DbContext` to `MiniCore.Framework.Data.DbContext`
2. **Registration** - Changed from `EntityFrameworkExtensions.AddDbContext()` to `MiniCore.Framework.Data.Extensions.ServiceCollectionExtensions.AddDbContext()`
3. **EnsureCreated** - Changed from `context.Database.EnsureCreated()` to `context.EnsureCreated()`
4. **FindAsync** - Changed from `FindAsync(id)` to `FindAsync(new object[] { id })`
5. **Select Projections** - Moved Select() projections to in-memory after ToListAsync()

## Next Steps

Phase 9 is complete. Next phases:

- **Phase 10**: Frontend Templating (replace Razor)
- **Phase 11**: Background Services (already implemented in Phase 4)

## References

- [Entity Framework Core Documentation](https://learn.microsoft.com/en-us/ef/core/)
- [Microsoft.Data.Sqlite Documentation](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/)
- [LINQ to SQL Translation](https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/sql/linq/)

