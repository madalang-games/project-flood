# server - ASP.NET Core 8 | C# | Entity Framework Core

## Stack
ASP.NET Core 8 Web API | C# | Entity Framework Core 8 (ORM only, no migrations) | Pomelo MySQL | StackExchange.Redis | JWT Bearer | Scalar

## Nav
| path | role |
|------|------|
| `db/` | DB schema definition + migration history | -> `db/AGENTS.md` |
| `src/ProjectFlood.sln` | Solution file |
| `src/ProjectFlood.Domain/` | Entities, interfaces, pure helpers | -> `src/ProjectFlood.Domain/AGENTS.md` |
| `src/ProjectFlood.Application/` | Use cases (commands/queries) | -> `src/ProjectFlood.Application/AGENTS.md` |
| `src/ProjectFlood.Infrastructure/` | EF Core DbContext, Redis, JWKS/auth clients | -> `src/ProjectFlood.Infrastructure/AGENTS.md` |
| `src/ProjectFlood.API/` | Startup, controllers, middleware, filters, Dockerfile | -> `src/ProjectFlood.API/AGENTS.md` |
| `tests/` | Engine-free server/API test projects | -> `tests/AGENTS.md` |
| `generated/` | Auto-generated - DO NOT edit |

## Rules
- NEVER edit `*/generated/*` - source is in `shared/`
- EF Core is used as ORM ONLY - never run `dotnet ef migrations` or `dotnet ef database update`
- DB schema managed by `npm run gen:orm` (reads `server/db/schema.json`)
- NEVER commit `.env.dev` or `.env.prod` - use `.env.dev.example` / `.env.prod.example`
- NEW_DIR: create `AGENTS.md` for it + update Nav above

## Project References
API -> Application -> Domain
Infrastructure -> Domain
API -> Infrastructure

## Auth Rules
- Project Flood validates platform access JWTs statelessly with JWKS.
- JWT `sub` is platform PID, never internal uid.
- `UserIdResolutionMiddleware` resolves PID to internal `user_id` claim before controllers run.
- Controllers and services never accept uid from request bodies.
- Platform-auth owns refresh, logout, session-family state, account identity, and token revocation.
- Game server does not maintain `sessions.active` or implement local session revocation.

## Conventions
- Namespaces: `ProjectFlood.{Layer}` or `ProjectFlood.{Layer}.{Domain}`
- No comments unless WHY is non-obvious
- `async/await` throughout - no `.Result` or `.Wait()`
- CancellationToken passed through all async methods
- Column names mapped to snake_case in `OnModelCreating`

## Cross-refs
| type | refs |
|------|------|
| Depends on | `docs/refs/platform-auth.md` |
| External API | `platform-auth:GET /.well-known/jwks.json`, `platform-auth:GET /api/internal/users/{pid}/uid` |
