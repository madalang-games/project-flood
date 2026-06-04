# ProjectFlood.Application/Common

## Files
| file | class | role |
|------|-------|------|
| `ErrorCodes.cs` | `ErrorCodes` | Stable client-visible error code constants |
| `GameApiException.cs` | `GameApiException` | Application exception carrying an error code |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `ErrorCodes.StaminaFull` | constant | Full life ad reward rejection |
| `ErrorCodes.InvalidStageAttempt` | constant | Missing/mismatched Redis attempt |
| `GameApiException.Code` | property | Serialized by API middleware |

## Rules
- Add new client-visible failures here before using them in services.
