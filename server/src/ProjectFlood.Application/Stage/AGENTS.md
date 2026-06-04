# ProjectFlood.Application/Stage

## Files
| file | class | role |
|------|-------|------|
| `StageAttemptService.cs` | `StageAttemptService` | Redis-backed active stage attempt lifecycle and revive ad handling |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `StageAttemptService.StartAsync` | method | Replaces existing active attempt without refund |
| `StageAttemptService.ClearAsync` | method | Clears attempt and refunds spent life |
| `StageAttemptService.FailAsync` | method | Fails attempt without refund |
| `StageAttemptService.ReviveAdAsync` | method | 3/2/1 turn revive ad sequence |

## Rules
- Only one active attempt per user.
- Redis loss is accepted; invalid attempt requests fail.
- Attempt TTL comes from `StaminaConfigProvider`.

## Cross-refs
- Depends on: `ProjectFlood.Application.Stamina.StaminaService`
- Depends on: `StackExchange.Redis.IDatabase`
- Consumed by: `ProjectFlood.API.Controllers.StageController`
