# ProjectFlood.API/Middleware

## Files
| file | class | role |
|------|-------|------|
| `CorrelationIdMiddleware.cs` | `CorrelationIdMiddleware` | Creates or propagates per-request correlation id |
| `ApiExceptionMiddleware.cs` | `ApiExceptionMiddleware` | Converts application exceptions to `ErrorResponse` |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `CorrelationIdMiddleware.InvokeAsync` | method | Writes `HttpContext.Items["CorrelationId"]` |
| `ApiExceptionMiddleware.InvokeAsync` | method | Maps `GameApiException.Code` to response body |

## Rules
- Keep error codes stable; clients branch on `ErrorResponse.Code`.

## Cross-refs
- Consumed by: `ProjectFlood.API.Program`
