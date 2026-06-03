# ProjectFlood.Application

## Nav
| path | role | link |
|------|------|------|
| `Bootstrap/` | App version + server time response | `Bootstrap/AGENTS.md` |
| `Session/` | `ISessionCache` interface + `SessionService` | `Session/AGENTS.md` |
| `Player/` | Player profile reads and updates | `Player/AGENTS.md` |

## Rules
- Use-case layer: persistence via direct `AppDbContext` injection (no repository interfaces)
- Services return contract DTOs (`ProjectFlood.Contracts.*`); never expose domain entities
- `async/await` throughout; CancellationToken passed to all async methods
- NEW_DIR: create `AGENTS.md` for it + update Nav above
