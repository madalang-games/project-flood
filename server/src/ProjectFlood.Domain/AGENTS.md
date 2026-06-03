# ProjectFlood.Domain

## Nav
| path | role |
|------|------|
| `Logging/` | EventLogIds, EventLogFactory, event log definitions |

## Logging Convention
All user-modifying API calls must produce an `event_logs` row. Currency/inventory changes linked via `correlation_id`.

| artifact | path |
|----------|------|
| TrId master document | `server/db/event_log_definitions.json` |
| TrId constants | `server/src/ProjectFlood.Domain/Logging/EventLogIds.cs` |
| Factory | `server/src/ProjectFlood.Domain/Logging/EventLogFactory.cs` |

## Rules
- No external dependencies — pure domain layer
- Entities and interfaces only
- No EF Core attributes; mapping done in Infrastructure `OnModelCreating`
- NEW_DIR: create `AGENTS.md` for it + update Nav above
