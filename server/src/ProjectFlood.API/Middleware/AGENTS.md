# ProjectFlood.API/Middleware

## Files
| file | class | role |
|------|-------|------|
| `CorrelationIdMiddleware.cs` | `CorrelationIdMiddleware` | Creates or propagates per-request correlation id |
| `ApiExceptionMiddleware.cs` | `ApiExceptionMiddleware` | Converts application and DB exceptions to `ErrorResponse` |
| `UserIdResolutionMiddleware.cs` | `UserIdResolutionMiddleware` | Resolves JWT `sub` platform PID to internal `user_id` claim |
| `VersionCheckMiddleware.cs` | `VersionCheckMiddleware` | Rejects unsupported client/protocol versions |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `CorrelationIdMiddleware.InvokeAsync` | method | Writes `HttpContext.Items["CorrelationId"]` |
| `ApiExceptionMiddleware.InvokeAsync` | method | Maps `GameApiException.Code` and DB concurrency conflicts |
| `UserIdResolutionMiddleware.InvokeAsync` | method | Creates first-seen `players` row with `InsertIgnoreAsync` |
| `VersionCheckMiddleware.InvokeAsync` | method | Requires `X-Client-Version` and `X-Protocol-Version` |

## Rules
- Keep error codes stable; clients branch on `ErrorResponse.Code`.
- No local session revocation or `sessions.active` checks.
- JWT `sub` is platform PID, not uid.

## Cross-refs
- Consumed by: `ProjectFlood.API.Program`
- Depends on: `ProjectFlood.Infrastructure.Security.PlatformAuthClient`
