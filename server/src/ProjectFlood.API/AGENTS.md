# ProjectFlood.API

## Nav
| path | role |
|------|------|
| `Controllers/` | All HTTP endpoints |
| `Middleware/` | Request pipeline: correlation ID, version check, player-id resolution, session validation, global exception handler |
| `Program.cs` | ASP.NET Core entry point: DI registrations, middleware order |
| `ProjectFloodConfiguration.cs` | Strict env/config loader for deploy/runtime values |
| `MockAuthenticationHandler.cs` | Development/mock auth scheme for local guest tokens |

## Rules
- `playerId` extracted from authenticated `player_id` claim in every authenticated controller; never from request body
- Unauthenticated endpoints: `GET /health`, `GET /api/bootstrap/config`, `POST /api/auth/guest`, `POST /api/auth/refresh`
- Middleware order in `Program.cs`: CorrelationId -> SerilogRequestLogging -> GlobalException -> HTTPS -> Auth -> PlayerIdResolution -> Authorization -> RateLimit -> VersionCheck -> SessionValidation -> Controllers
- `LOG_LEVEL` env var overrides `appsettings.*.json` Serilog minimum level
- NEW_DIR: create `AGENTS.md` for it + update Nav above
