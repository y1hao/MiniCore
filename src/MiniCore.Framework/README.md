# MiniCore.Framework

The core framework implementation for MiniCore, progressively replacing Microsoft's ASP.NET Core implementations.

## Structure

```
MiniCore.Framework/
├── DI/                    # Dependency Injection (Phase 1)
│   ├── Extensions/        # Extension methods
│   └── ...
├── Config/                # Configuration (Phase 2)
├── Logging/               # Logging (Phase 3)
├── Hosting/               # Host abstraction (Phase 4)
├── Server/                # HTTP Server (Phase 7)
├── Routing/               # Routing (Phase 6)
├── Middleware/            # Middleware pipeline (Phase 5)
└── Background/            # Background services (Phase 10)
```

## Current Status

### Phase 1: Dependency Injection (In Progress)
- [x] Project setup
- [ ] Core interfaces
- [ ] Service collection implementation
- [ ] Service provider implementation
- [ ] Service scope implementation
- [ ] Extension methods
- [ ] Testing

## Usage

This framework is designed to be a drop-in replacement for Microsoft's implementations, maintaining API compatibility while providing custom implementations.

