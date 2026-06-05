# shared/contracts/Stage

## Files
| file | class | role |
|------|-------|------|
| `StageRequests.cs` | `StageAttemptStartRequest`, `StageAttemptClearRequest`, `StageAttemptFailRequest`, `StageReviveAdRequest` | Stage attempt request DTOs |
| `StageResponses.cs` | `StageAttemptSnapshot`, `StageAttemptStartResponse`, `StageAttemptEndResponse`, `StageReviveAdResponse` | Stage attempt response DTOs |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `StageAttemptSnapshot.AttemptId` | property | Server-generated attempt id required for clear/fail/revive |
| `StageAttemptClearRequest.RulesetVersion` | property | Client stage ruleset version used by server validation |
| `StageAttemptClearRequest.TurnsUsed` | property | Server-authoritative ranking input after validation |
| `StageAttemptClearRequest.RemainingBasicCells` | property | Summary input for server star validation |
| `StageAttemptClearRequest.CoreRemaining` | property | Clear is invalid while a core cell remains |
| `StageAttemptEndResponse.Stars` | property | Server-computed clear stars |
| `StageAttemptEndResponse.StageRank` | property | Current per-stage competition rank from ranking cache/DB |
| `StageAttemptEndResponse.IsNewBest` | property | True when `TurnsUsed` improves the player's stage best turns |
| `StageReviveAdResponse.TurnsGranted` | property | 3, 2, 1 by revive count |

## Rules
- DTOs describe HTTP boundary only; Redis attempt storage shape is server-internal.

## Cross-refs
- Consumed by: `ProjectFlood.API.Controllers.StageController`
- Consumed by: `ProjectFlood.Application.Stage.StageAttemptService`
