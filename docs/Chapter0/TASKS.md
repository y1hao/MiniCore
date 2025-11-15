# Chapter 0 Tasks — Reference URL Shortener

Plan for building the baseline ASP.NET Core app that we will later strangle with MiniCore.

## Build Plan
- **Bootstrap**: Create solution/project via `dotnet new web`; add SQLite connection string to `appsettings.json`; wire EF Core packages.
- **Domain & DbContext**: Define `ShortLink` entity (Id, ShortCode, OriginalUrl, CreatedAt, ExpiresAt?, VisitCount); configure indexes and constraints; add migrations and initial database.
- **Short Code Generation**: Implement Base62 generator with configurable length; support user-supplied custom codes with charset/length validation; collision retry with bounded attempts.
- **Endpoints**: Implement `GET /api/links`, `POST /api/links` (validate URL/expiry/custom code), `DELETE /api/links/{id}`, `GET /{shortCode}` redirect with 302/404/410 and visit count increment.
- **Admin UI**: Razor/MVC view at `/admin` listing links; form for create (with optional custom code/expiry); delete buttons posting to API.
- **Background Cleanup**: Hosted service running on interval (default hourly) removing or marking expired links; logs stats.
- **Middleware & Logging**: Add exception handler, request logging, routing; structured logs for CRUD, redirects, collisions, cleanup.
- **Configuration**: Short code length/charset, cleanup interval, logging levels; ensure options bound from config/environment.
- **Testing**: Unit/integration tests for API validation, redirect behavior (status/location + visit count), collision handling, cleanup service effects.

## Deliverables
- Working ASP.NET Core project with above endpoints/UI/background job.
- SQLite database and initial migration.
- Tests covering happy-path and key failure cases (collision/validation/expiry).
- Docs: updated `README.md` (design) and this tasks list.
