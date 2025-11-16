# Phase 0: Baseline Application

## Overview

Phase 0 establishes the foundation for the MiniCore project by creating a fully functional URL shortener application using standard ASP.NET Core. This baseline application serves as both a reference implementation and a test target for all subsequent phases where we'll progressively replace ASP.NET Core components with custom implementations.

## Objectives

The primary goals of Phase 0 were to:

1. **Create a realistic web application** that exercises core ASP.NET Core features
2. **Establish a reference baseline** for functional parity testing
3. **Build comprehensive test coverage** to validate behavior throughout the project
4. **Document the application** for future reference and comparison

## Deliverables

### Projects Created

#### 1. MiniCore.Web
The main application project - a fully functional URL shortener built with ASP.NET Core.

**Key Features:**
- RESTful API endpoints for link management
- MVC controllers and Razor views
- Entity Framework Core with SQLite persistence
- Background service for automatic cleanup
- Dependency injection throughout
- Configuration management
- Comprehensive logging

**Documentation:** See [MiniCore.Web README](../../src/MiniCore.Web/README.md) for complete details.

#### 2. MiniCore.Web.Tests
Comprehensive test suite for the baseline application.

**Test Coverage:**
- **Unit Tests**: Controller tests with mocked dependencies
  - `AdminControllerTests` - Admin UI functionality
  - `RedirectControllerTests` - Redirect endpoint behavior
  - `ShortLinkControllerTests` - API endpoint tests
  - `LinkCleanupServiceTests` - Background service tests
- **Integration Tests**: Full API integration tests
  - `ApiIntegrationTests` - End-to-end API testing

**Total:** 42 passing tests covering all major functionality

#### 3. MiniCore.Reference
A **static reference copy** of MiniCore.Web that will remain unchanged throughout the project.

**Purpose:**
- Provides a frozen baseline for comparison
- Allows verification that custom implementations maintain compatibility
- Serves as a reference when implementing custom components

**Important:** This project should **NOT** be modified. It exists solely as a reference point.

**Documentation:** See [MiniCore.Reference README](../../src/MiniCore.Reference/README.md) for details.

#### 4. MiniCore.Reference.Tests
Test suite for the reference implementation, ensuring it remains functional and unchanged.

## Application Architecture

### API Endpoints

The application provides the following REST API endpoints:

#### `GET /api/links`
Retrieves a paginated list of all short links.

**Query Parameters:**
- `page` (optional, default: 1) - Page number
- `pageSize` (optional, default: 50) - Number of items per page

**Response:** `200 OK` with array of link objects

#### `POST /api/links`
Creates a new short link.

**Request Body:**
```json
{
  "originalUrl": "https://example.com",
  "shortCode": "custom-code",  // Optional
  "expiresAt": "2024-12-31T00:00:00Z"  // Optional
}
```

**Response:** `201 Created` with created link object

**Error Responses:**
- `400 Bad Request` - Invalid URL format or validation errors
- `409 Conflict` - Short code already exists

#### `DELETE /api/links/{id}`
Deletes a short link by ID.

**Response:** `204 No Content`

**Error Responses:**
- `404 Not Found` - Link with specified ID not found

#### `GET /{shortCode}`
Redirects to the original URL associated with the short code.

**Features:**
- Path preservation: Additional path segments are preserved (e.g., `/abc123/path/to/resource`)
- Expiration checking: Expired links return 404
- Case-sensitive matching

**Response:** `302 Found` (redirect)

**Error Responses:**
- `404 Not Found` - Short code not found or expired

#### `GET /admin`
Renders the admin interface HTML page for managing links.

**Response:** `200 OK` (HTML)

### Project Structure

```
MiniCore.Web/
├── Controllers/
│   ├── ShortLinkController.cs    # REST API for link management
│   ├── RedirectController.cs      # Handles short code redirects
│   └── AdminController.cs         # Admin UI controller
├── Models/
│   ├── ShortLink.cs              # Entity model
│   └── ShortLinkDto.cs           # DTOs for API responses
├── Data/
│   └── AppDbContext.cs           # EF Core DbContext
├── Services/
│   └── LinkCleanupService.cs     # Background service for cleanup
├── Views/
│   └── Admin/
│       └── Index.cshtml          # Admin UI view
├── wwwroot/
│   ├── css/
│   │   └── admin.css            # Admin page styles
│   └── js/
│       └── admin.js             # Admin page JavaScript
├── Program.cs                    # Application entry point
└── appsettings.json             # Configuration
```

### Key Components

#### Controllers
- **ShortLinkController**: Handles CRUD operations for short links via REST API
- **RedirectController**: Implements the redirect endpoint with path preservation
- **AdminController**: Serves the admin UI view

#### Models
- **ShortLink**: Entity model representing a short link with properties:
  - `Id`: Primary key
  - `ShortCode`: Unique short code (1-20 characters)
  - `OriginalUrl`: The original URL to redirect to
  - `CreatedAt`: Creation timestamp
  - `ExpiresAt`: Optional expiration date
- **ShortLinkDto**: Data transfer object for API responses
- **CreateShortLinkRequest**: Request model for creating links

#### Data Layer
- **AppDbContext**: Entity Framework Core DbContext with:
  - SQLite database configuration
  - Unique index on `ShortCode`
  - Automatic database creation via `EnsureCreated()`

#### Services
- **LinkCleanupService**: Background service that:
  - Runs periodically (configurable interval, default: 1 hour)
  - Removes expired links automatically
  - Logs cleanup operations

#### Views
- **Admin/Index.cshtml**: Razor view for the admin interface with:
  - Form to create new short links
  - Table displaying all links
  - Delete functionality
  - Expiration status display

### Features Implemented

1. **Short Code Generation**
   - Automatic generation using SHA256 hash of URL + timestamp
   - 8-character codes, URL-safe characters only
   - Uniqueness guaranteed through collision detection

2. **Custom Short Codes**
   - Optional user-specified codes (1-20 characters)
   - Validation: alphanumeric, hyphens, underscores only
   - Uniqueness checking

3. **Expiration Management**
   - Optional expiration dates for links
   - Automatic cleanup via background service
   - Configurable default expiration period

4. **Path Preservation**
   - Additional path segments after short code are preserved
   - Example: `/abc123/api/users` → `{originalUrl}/api/users`

5. **Admin Interface**
   - Web-based UI for managing links
   - Real-time creation and deletion
   - Visual indication of expired links

## Technology Stack

- **Framework**: ASP.NET Core 10.0
- **Database**: SQLite with Entity Framework Core
- **Templating**: Razor Views
- **Testing**: xUnit with Moq for mocking
- **Build**: .NET SDK 10.0

## Configuration

The application uses standard ASP.NET Core configuration:

- **Connection Strings**: SQLite database location
- **Link Cleanup Settings**: Interval and default expiration
- **Logging**: Standard ASP.NET Core logging configuration

See the project READMEs for detailed configuration examples.

## Testing Strategy

### Test Coverage

The test suite includes:

1. **Unit Tests** (with mocked dependencies):
   - Controller action testing
   - Service method testing
   - Edge case handling

2. **Integration Tests**:
   - Full HTTP request/response cycle
   - Database integration
   - End-to-end API workflows

### Test Results

- **Total Tests**: 42
- **Status**: All passing
- **Coverage**: All major functionality covered

### Running Tests

```bash
# From solution root
dotnet test

# Run specific test project
dotnet test src/MiniCore.Web.Tests/MiniCore.Web.Tests.csproj

# Run with verbose output
dotnet test --verbosity normal
```

## Relationship Between Projects

### MiniCore.Web vs MiniCore.Reference

- **MiniCore.Web**: The active project that will evolve as we replace ASP.NET Core components
- **MiniCore.Reference**: Static copy that remains unchanged for comparison

As we progress through phases:
1. `MiniCore.Web` will gradually use custom implementations
2. `MiniCore.Reference` will remain unchanged
3. Tests can compare behavior between the two
4. We can verify functional parity at each phase

## What This Phase Demonstrates

Phase 0 successfully demonstrates all the ASP.NET Core concepts we'll re-implement:

1. **Dependency Injection**: Controllers, services, and DbContext use constructor injection
2. **Configuration**: App settings, connection strings, and feature flags
3. **Logging**: Structured logging throughout the application
4. **Hosting**: Application lifecycle and startup/shutdown
5. **Middleware Pipeline**: Request processing pipeline
6. **Routing**: Attribute-based and convention-based routing
7. **HTTP Server**: Request/response handling (via Kestrel)
8. **ORM**: Entity Framework Core for data access
9. **Templating**: Razor views for server-side rendering
10. **Background Services**: Long-running tasks with lifecycle management

## Next Steps

With Phase 0 complete, we're ready to begin Phase 1: **Dependency Injection Framework**.

The baseline application provides:
- ✅ A working reference implementation
- ✅ Comprehensive test coverage
- ✅ Clear examples of all concepts to re-implement
- ✅ A static reference copy for comparison

## Related Documentation

- [Project Specification](../../SPEC.md) - Overall project goals and phases
- [MiniCore.Web README](../../src/MiniCore.Web/README.md) - Detailed application documentation
- [MiniCore.Reference README](../../src/MiniCore.Reference/README.md) - Reference project documentation

## Summary

Phase 0 successfully establishes:

- ✅ A fully functional URL shortener application
- ✅ Complete test coverage (42 tests, all passing)
- ✅ Static reference copy for comparison
- ✅ Comprehensive documentation
- ✅ Foundation for progressive component replacement

The application is production-ready and serves as an excellent baseline for understanding ASP.NET Core's architecture before we begin replacing its components with custom implementations.

