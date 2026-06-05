# ProjectFlood.Application

## Nav
| path | role | link |
|------|------|------|
| `Common/` | Error codes and API exception type | `Common/AGENTS.md` |
| `Logging/` | Event log row factory | `Logging/AGENTS.md` |
| `Stamina/` | Stamina config, life, regen, ad life, unlimited state | `Stamina/AGENTS.md` |
| `Stage/` | Redis stage attempt lifecycle and revive ads | `Stage/AGENTS.md` |
| `Rewards/` | Generic reward source and ad reward claim services | `Rewards/AGENTS.md` |

## Rules
- Use-case layer: persistence via direct `AppDbContext` injection.
- Services return contract DTOs (`ProjectFlood.Contracts.*`); never expose generated DB rows.
- Controllers provide internal `user_id`; services never resolve JWT claims.
- `async/await` throughout; CancellationToken passed to all async methods.
- NEW_DIR: create `AGENTS.md` for it + update Nav above.
