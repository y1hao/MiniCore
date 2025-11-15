# Chapter 0 - Baseline URL Shortener (Reference App)

This document captures the system design for the reference ASP.NET Core URL shortener we will strangle-replace with MiniCore. It summarizes the domain model, endpoints, background processing, configuration, logging, and testing targets.

## Domain Model
- **ShortLink**: `Id (GUID or int)`, `ShortCode (unique)`, `OriginalUrl`, `CreatedAt`, `ExpiresAt?`, `VisitCount`.
- Persistence: SQLite via EF Core. Index `ShortCode`; optional index `ExpiresAt` for cleanup queries.
- Behaviour: reject invalid URLs, enforce scheme `http/https`, optional expiry, increment visits on redirect.

## API Surface
- `GET /api/links`: list all links (optionally omit expired).
- `POST /api/links`: create link; validate URL and expiry; generate unique short code; return created resource.
- `DELETE /api/links/{id}`: delete link by Id (hard delete is fine; soft delete optional).
- `GET /{shortCode}`: redirect to `OriginalUrl` with 302; return 404/410 for missing/expired; increment `VisitCount` on success.

## Admin UI
- Simple Razor/MVC view at `/admin` rendering a table: code, target URL, expiry, visits, created time, delete action.
- Interacts with the same API endpoints for create/delete; no extra backend logic beyond view rendering.

## Short Code Generation
- Base62 random 6–8 characters (length configurable).
- Retry on collision; log collisions; bounded retry count.
- Allow custom code input (validated for charset/length); prefer random when not provided.

## Background Processing
- Hosted service runs hourly (configurable) to delete or mark expired links and optionally compact the SQLite DB.
- Logs counts of processed/remaining items; should be idempotent and resilient to failures.

## Configuration
- Connection string (SQLite file path).
- Short code length and allowed charset.
- Cleanup interval and expiry grace handling.
- Logging level.

## Logging & Observability
- Structured logs for create/delete/redirect events, collisions, validation failures, and cleanup runs.
- Include request id/correlation id where available; log redirect target only at INFO (avoid in DEBUG if privacy a concern).

## Middleware & Routing
- Standard ASP.NET Core pipeline: exception handler → request logging → routing → endpoints.
- No static files required unless the admin UI pulls assets; keep cache headers conservative on admin (`no-cache`).

## Testing Targets
- API happy paths and validation failures.
- Redirect returns correct status/location and increments `VisitCount`.
- Cleanup removes/marks expired links.
- Collision retry succeeds when generator is forced to collide (mockable generator).

## Non-functional Considerations
- Idempotent deletes; consistent 404/410 for missing vs expired.
- HTTPS-friendly redirects (preserve scheme; avoid open redirects by validating origin format only without whitelisting).
- Input limits on URL length and code length.
- Minimal caching on admin to avoid stale listings.
