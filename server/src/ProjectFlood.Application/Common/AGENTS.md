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
| `ErrorCodes.StageNotFound` | constant | Clear validation stage lookup failure |
| `ErrorCodes.StageRulesetMismatch` | constant | Client clear request ruleset does not match server data |
| `ErrorCodes.InvalidStageClear` | constant | Invalid clear summary or insufficient clear ratio |
| `ErrorCodes.InvalidRankingType` | constant | Unknown global ranking type route |
| `GameApiException.Code` | property | Serialized by API middleware |

## Rules
- Add new client-visible failures here before using them in services.
