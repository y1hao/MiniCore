# MiniCore.Reference

A **static reference copy** of the baseline URL shortener application. This project is an exact copy of `MiniCore.Web` at the time of creation and will remain **unchanged** throughout the MiniCore project evolution.

## Purpose

MiniCore.Reference serves as a **frozen reference implementation** that preserves the original baseline application (Phase 0) for the MiniCore project - an educational effort to re-implement ASP.NET Core from scratch.

**Important**: This project should **NOT** be modified. It exists solely as a reference point to compare against `MiniCore.Web` as we gradually replace ASP.NET Core's underlying components with custom implementations.

## Relationship to MiniCore.Web

- **MiniCore.Reference**: Static, unchanging reference copy (this project)
- **MiniCore.Web**: Active project that will evolve to use custom MiniCore implementations

As `MiniCore.Web` evolves, `MiniCore.Reference` will remain unchanged, allowing us to:
- Compare behavior between the reference and evolving implementations
- Verify that our custom implementations maintain compatibility
- Reference the original baseline implementation when needed

## Features

This is a fully functional URL shortener that demonstrates core ASP.NET Core concepts including:
- RESTful API endpoints
- MVC controllers and views
- Entity Framework Core with SQLite
- Background services
- Dependency injection
- Configuration management
- Logging

### Feature Details

- **Short Link Management**: Create, list, and delete shortened URLs
- **Automatic Short Code Generation**: SHA256-based 8-character codes
- **Custom Short Codes**: Optionally specify your own short codes (1-20 characters, alphanumeric with hyphens/underscores)
- **Expiration Support**: Links can have optional expiration dates
- **Path Preservation**: Redirects preserve additional path segments (e.g., `/abc123/path/to/resource`)
- **Automatic Cleanup**: Background service removes expired links periodically
- **Admin Interface**: Web-based admin page to view and manage all links
- **Pagination**: API supports pagination for listing links

## Project Structure

```
MiniCore.Reference/
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

## API Design

### Endpoints

#### `GET /api/links`
Retrieves a paginated list of all short links.

**Query Parameters:**
- `page` (optional, default: 1) - Page number
- `pageSize` (optional, default: 50) - Number of items per page

**Response:** `200 OK`
```json
[
  {
    "id": 1,
    "shortCode": "abc123",
    "originalUrl": "https://example.com",
    "createdAt": "2024-01-01T00:00:00Z",
    "expiresAt": "2024-01-31T00:00:00Z",
    "shortUrl": "https://localhost:5000/abc123"
  }
]
```

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

**Response:** `201 Created`
```json
{
  "id": 1,
  "shortCode": "custom-code",
  "originalUrl": "https://example.com",
  "createdAt": "2024-01-01T00:00:00Z",
  "expiresAt": "2024-12-31T00:00:00Z",
  "shortUrl": "https://localhost:5000/custom-code"
}
```

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

**Path Parameters:**
- `shortCode` - The short code (can include additional path segments)

**Response:** `302 Found` (redirect)

**Error Responses:**
- `404 Not Found` - Short code not found or expired

**Path Preservation:**
If the short code is followed by additional path segments, they are preserved:
- `/abc123` → redirects to original URL
- `/abc123/path/to/resource` → redirects to `{originalUrl}/path/to/resource`

#### `GET /admin`
Renders the admin interface HTML page for managing links.

**Response:** `200 OK` (HTML)

## Configuration

Configuration is managed through `appsettings.json` and `appsettings.Development.json`.

### Connection Strings
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=minicore.db"
  }
}
```

### Link Cleanup Settings
```json
{
  "LinkCleanup": {
    "IntervalHours": 1,
    "DefaultExpirationDays": 30
  }
}
```

- `IntervalHours`: How often the cleanup service runs (default: 1 hour)
- `DefaultExpirationDays`: Default expiration period for links without explicit expiration (default: 30 days)

### Logging
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download) or later
- SQLite (included with .NET runtime)

## Running the Application

### Development Mode

1. Navigate to the project directory:
   ```bash
   cd src/MiniCore.Reference
   ```

2. Restore dependencies:
   ```bash
   dotnet restore
   ```

3. Run the application:
   ```bash
   dotnet run
   ```

   Or use a specific profile:
   ```bash
   dotnet run --launch-profile https
   ```

4. The application will start on:
   - HTTP: `http://localhost:5037`
   - HTTPS: `https://localhost:7133`

5. Access the admin interface at:
   - `http://localhost:5037/admin` or `https://localhost:7133/admin`

### Database

The SQLite database (`minicore.db`) is automatically created on first run if it doesn't exist. The database is located in the project root directory.

## Testing

### Running Tests

Tests are located in the `MiniCore.Reference.Tests` project. Run all tests with:

```bash
# From solution root
dotnet test

# Or from the test project directory
cd src/MiniCore.Reference.Tests
dotnet test
```

### Test Structure

The test suite includes:
- **Unit Tests**: Controller tests with mocked dependencies
- **Integration Tests**: API integration tests

Tests use an in-memory database to avoid affecting the development database.

### Example Test Commands

```bash
# Run all tests
dotnet test

# Run tests with verbose output
dotnet test --verbosity normal

# Run a specific test class
dotnet test --filter "FullyQualifiedName~ShortLinkControllerTests"

# Run tests and collect code coverage
dotnet test /p:CollectCoverage=true
```

## Usage Examples

### Creating a Short Link

**Using curl:**
```bash
curl -X POST https://localhost:7133/api/links \
  -H "Content-Type: application/json" \
  -d '{
    "originalUrl": "https://example.com",
    "shortCode": "example",
    "expiresAt": "2024-12-31T00:00:00Z"
  }'
```

**Using PowerShell:**
```powershell
Invoke-RestMethod -Uri "https://localhost:7133/api/links" `
  -Method Post `
  -ContentType "application/json" `
  -Body '{
    "originalUrl": "https://example.com",
    "shortCode": "example"
  }'
```

### Listing Links

```bash
curl https://localhost:7133/api/links?page=1&pageSize=10
```

### Accessing a Short Link

Simply navigate to `https://localhost:7133/{shortCode}` in your browser or use curl:

```bash
curl -L https://localhost:7133/example
```

The `-L` flag follows redirects.

## Background Services

The `LinkCleanupService` runs as a background service and automatically removes expired links. It:
- Runs periodically based on `LinkCleanup:IntervalHours` configuration
- Removes links where `ExpiresAt < DateTime.UtcNow`
- Logs cleanup operations

The service starts automatically when the application starts and stops gracefully when the application shuts down.

## Short Code Generation

When a custom short code is not provided, the system generates an 8-character code using:
1. SHA256 hash of the original URL + current timestamp
2. Base64 encoding (with special characters removed)
3. First 8 characters of the result

This ensures:
- Deterministic generation (same URL + timestamp = same code)
- Uniqueness (timestamp ensures variation)
- URL-safe characters only

## Development Notes

- The application uses Entity Framework Core migrations implicitly via `EnsureCreated()`
- Static files are served from the `wwwroot` directory
- The redirect endpoint uses a fallback route pattern to catch all unmatched routes
- Testing environment is detected via `IsEnvironment("Testing")` to avoid database conflicts

## Troubleshooting

### Database Issues
- If the database file is locked, ensure no other process is using it
- Delete `minicore.db` to reset the database (all data will be lost)

### Port Already in Use
- Change the port in `Properties/launchSettings.json`
- Or set the `ASPNETCORE_URLS` environment variable

### HTTPS Certificate Errors
- In development, you may need to trust the development certificate:
  ```bash
  dotnet dev-certs https --trust
  ```

## Related Documentation

- See `SPEC.md` in the project root for the overall MiniCore project specification
- This is Phase 0 (Baseline Application) of the MiniCore implementation plan
- Compare with `MiniCore.Web` to see the evolution of the implementation

## License

This project is part of an educational effort to understand ASP.NET Core internals.
