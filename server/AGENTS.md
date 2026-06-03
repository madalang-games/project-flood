# server — ASP.NET Core 8 | C# | Entity Framework Core

## Stack
ASP.NET Core 8 Web API | C# | Entity Framework Core 8 (ORM only, no migrations) | Pomelo MySQL | StackExchange.Redis | JWT Bearer | Scalar

## Nav
| path | role |
|------|------|
| `db/` | DB schema definition + migration history | → `db/AGENTS.md` |
| `src/ProjectFlood.sln` | Solution file |
| `src/ProjectFlood.Domain/` | Entities, interfaces — no dependencies |
| `src/ProjectFlood.Application/` | Use cases (commands/queries), ISessionCache |
| `src/ProjectFlood.Infrastructure/` | EF Core DbContext, repositories, Redis cache, JWT key cache |
| `src/ProjectFlood.API/` | Startup, controllers, middleware, Dockerfile |
| `tests/` | Engine-free server/API test projects | → `tests/AGENTS.md` |
| `generated/` | Auto-generated — DO NOT edit |

## Rules
- NEVER edit `*/generated/*` — source is in `shared/`
- EF Core is used as ORM ONLY — never run `dotnet ef migrations` or `dotnet ef database update`
- DB schema managed by `npm run gen:orm` (reads `server/db/schema.json`)
- NEVER commit `.env.dev` or `.env.prod` — use `.env.dev.example` / `.env.prod.example`
- NEW_DIR: create `AGENTS.md` for it + update Nav above

## Project References
API → Application → Domain
Infrastructure → Domain
API → Infrastructure

## Conventions
- Namespaces: `ProjectFlood.{Layer}` or `ProjectFlood.{Layer}.{Domain}`
- No comments unless WHY is non-obvious
- `async/await` throughout — no `.Result` or `.Wait()`
- CancellationToken passed through all async methods
- Column names mapped to snake_case in `OnModelCreating`

## Cross-refs
| type | refs |
|------|------|
| Depends on | `docs/refs/platform-auth.md` |
| External API | `platform-auth:GET /.well-known/jwks.json`, `platform-auth:POST /auth/refresh` |
| Auth mode | `AUTH_USE_MOCK=true` → `MockAuthenticationHandler`; `false` → `JwtPublicKeyCache` + JWKS |
