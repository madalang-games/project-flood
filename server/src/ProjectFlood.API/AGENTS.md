# ProjectFlood.API

## Nav
| path | role |
|------|------|
| `Controllers/` | All HTTP endpoints | -> `Controllers/AGENTS.md` |
| `Middleware/` | Request pipeline: correlation ID, exception mapping, user-id resolution, version check | -> `Middleware/AGENTS.md` |
| `Filters/` | MVC action filters, including per-user write serialization | -> `Filters/AGENTS.md` |
| `Program.cs` | ASP.NET Core entry point: DI registrations, middleware order |
| `RankingRebuildHostedService.cs` | Startup Redis ranking rebuild from DB |
| `ProjectFloodConfiguration.cs` | Strict env/config loader for deploy/runtime values |
| `UserClaims.cs` | Claim helpers for internal `user_id` and platform PID |
| `ShortSourceContextEnricher.cs` | Serilog enricher that shortens SourceContext values to class names |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `UserClaims.UserId` | const | Internal server-side uid claim added by `UserIdResolutionMiddleware` |
| `UserClaims.PlatformPid` | const | JWT `sub`; platform PID, not numeric uid |
| `ProjectFloodConfiguration.RateLimit.StageStartPerHour` | property | Stage-start limiter setting |
| `RankingRebuildHostedService.ExecuteAsync` | method | Rebuilds Redis ranking keys on init; logs warning without blocking startup |

## Rules
- JWT `sub` is platform PID; controllers read internal `user_id` claim added by `UserIdResolutionMiddleware`.
- Unauthenticated endpoints: `GET /health` when present; auth/refresh/logout are platform-auth responsibilities.
- Middleware order in `Program.cs`: CorrelationId -> SerilogRequestLogging -> ApiException -> Auth -> UserIdResolution -> Authorization -> RateLimit -> VersionCheck -> Controllers.
- No `SessionValidationMiddleware`; Project Flood has no local session active/revocation state.
- `LOG_LEVEL` env var overrides `appsettings.*.json` Serilog minimum level.
- NEW_DIR: create `AGENTS.md` for it + update Nav above.
