# ProjectFlood.Infrastructure

## Nav
| path | role |
|------|------|
| `Generated/` | Auto-generated EF Core entities + AppDbContext (DO NOT EDIT) |
| `Persistence/` | Repository implementations |
| `Cache/` | Redis cache implementations |
| `Auth/` | JWT key cache + JWKS client |
| `StaticData/` | StaticDataService implementation |

## Rules
- NEVER edit `Generated/` — re-run `npm run gen:orm`
- Column → snake_case mapping done in `AppDbContext.OnModelCreating`
- NEW_DIR: create `AGENTS.md` for it + update Nav above
