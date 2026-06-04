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
| `StageReviveAdResponse.TurnsGranted` | property | 3, 2, 1 by revive count |

## Rules
- DTOs describe HTTP boundary only; Redis attempt storage shape is server-internal.

## Cross-refs
- Consumed by: `ProjectFlood.API.Controllers.StageController`
- Consumed by: `ProjectFlood.Application.Stage.StageAttemptService`
