# ProjectFlood.Application/Stage

## Files
| file | class | role |
|------|-------|------|
| `StageAttemptService.cs` | `StageAttemptService` | Redis-backed active stage attempt lifecycle, clear validation, ranking integration, and revive ad handling |
| `AdInterstitialService.cs` | `AdInterstitialService` | Interstitial eligibility (cooldown) and shown recording |
| `AdDoubleRewardService.cs` | `AdDoubleRewardService` | Double reward ad claim for stage clear (2x soft currency) |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `StageAttemptService.StartAsync` | method | Replaces existing active attempt without refund |
| `StageAttemptService.ClearAsync` | method | Validates clear summary, computes stars, records ranking DB state, refunds life, grants stage clear reward, sets Redis double_reward_eligible TTL 5min |
| `StageAttemptService.FailAsync` | method | Fails attempt without refund |
| `StageAttemptService.ReviveAdAsync` | method | 3/2/1 turn revive ad sequence |
| `StageAttemptService.DoubleRewardEligibleKey` | method | `double_reward_eligible:{userId}:{stageId}` static key helper |
| `AdInterstitialService.GetEligibilityAsync` | method | Returns cooldown state for INTERSTITIAL_POST_STAGE |
| `AdInterstitialService.RecordShownAsync` | method | Upserts `user_interstitial_state.last_shown_at` |
| `AdDoubleRewardService.ClaimAsync` | method | Validates eligibility key, verifies SSV, grants 2x reward group, returns InterstitialSuppressed=true |

## Rules
- Only one active attempt per user.
- Redis loss is accepted; invalid attempt requests fail.
- Attempt TTL comes from `StaminaConfigProvider`.
- Clear validation uses `ruleset_version`, `turns_used`, `remaining_basic_cells`, and `core_remaining` against server-side stage static data.
- Stage clear reward: granted only on first clear (`UserStageProgress.FirstClearedAt == null`); look up `stage.reward_group_id`, call `RewardService.GrantRewardGroupAsync`, then set Redis eligibility key. Retries earn no reward.
- Double reward: consumes eligibility key on success; `InterstitialSuppressed=true` signals client to skip next interstitial.

## Cross-refs
- Depends on: `ProjectFlood.Application.Stamina.StaminaService`
- Depends on: `ProjectFlood.Application.Rewards.RewardService`
- Depends on: `ProjectFlood.Application.Ranking.RankingService`
- Depends on: `StackExchange.Redis.IDatabase`
- Depends on: `shared/datas/stage/stage.csv` (reward_group_id, validation fields)
- Depends on: `shared/datas/ad/ad_placement.csv` (cooldown_seconds, min_stage)
- Consumed by: `ProjectFlood.API.Controllers.StageController`
- Consumed by: `ProjectFlood.API.Controllers.AdController`
