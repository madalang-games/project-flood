# ProjectFlood.Application/Ranking

## Files
| file | class | role |
|------|-------|------|
| `RankingService.cs` | `RankingService` | DB ranking aggregate updates, Redis index reads/writes, rebuilds |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `RankingService.RecordClearAsync` | method | Mutates DB progress/totals/clear record inside caller transaction |
| `RankingService.GetStageRankAsync` | method | Competition rank = better best-turn count + 1; Redis read with DB fallback |
| `RankingService.QueueRedisUpdate` | method | Fire-and-forget Redis update after DB commit |
| `RankingService.GetGlobalPageAsync` | method | Paged global `stars` or `max-stage` ranking |
| `RankingService.GetMyGlobalRankAsync` | method | Current user's global ranking card |
| `RankingService.GetMyStageRankAsync` | method | Current user's per-stage best turns and rank |
| `RankingService.RebuildAllAsync` | method | Rebuilds Redis ranking keys from DB |
| `StageClearEvaluation` | record | Server-computed stars and total basic cells |
| `StageClearRankingResult` | record | DB update result used by clear response and Redis update |

## Rules
- DB is source of truth; Redis is rebuildable cache/index.
- Redis write must happen after DB commit.
- Stage ranking uses competition ranking by lower `best_turns_used`.
- Global rankings use deterministic tie-break by earlier achieved timestamp.

## Cross-refs
- Depends on: `ProjectFlood.Infrastructure.Generated.AppDbContext`
- Depends on: `StackExchange.Redis.IDatabase`
- Consumed by: `ProjectFlood.Application.Stage.StageAttemptService`
- Consumed by: `ProjectFlood.API.Controllers.RankingController`
- Consumed by: `ProjectFlood.API.RankingRebuildHostedService`
