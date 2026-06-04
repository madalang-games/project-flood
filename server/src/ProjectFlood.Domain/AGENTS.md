# ProjectFlood.Domain

## Nav
| path | role |
|------|------|
| `Logging/` | EventLogIds constants |

## Logging Convention
All user-modifying API calls must produce an `event_logs` row. Currency, inventory, stamina, reward, and ad changes link related records with `correlation_id`.

| artifact | path |
|----------|------|
| TrId master document | `server/db/event_log_definitions.json` |
| TrId constants | `server/src/ProjectFlood.Domain/Logging/EventLogIds.cs` |
| Factory | `server/src/ProjectFlood.Application/Logging/EventLogFactory.cs` |

## Rules
- No external dependencies; pure domain layer.
- Entities and interfaces only.
- No EF Core attributes; mapping is generated in Infrastructure.
- NEW_DIR: create `AGENTS.md` for it + update Nav above.
