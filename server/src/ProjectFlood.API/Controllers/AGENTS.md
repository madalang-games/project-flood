# ProjectFlood.API/Controllers

## Files
| file | class | role |
|------|-------|------|
| `ControllerBaseEx.cs` | `ControllerBaseEx` | Shared player id and correlation id helpers |
| `StaminaController.cs` | `StaminaController` | `/api/stamina` status and ad life reward endpoints |
| `StageController.cs` | `StageController` | Stage attempt start/clear/fail/revive endpoints |
| `RewardsController.cs` | `RewardsController` | Generic reward source listing and claim endpoints |
| `AdRewardsController.cs` | `AdRewardsController` | Generic rewarded-ad claim endpoint |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `ControllerBaseEx.PlayerId` | property | Reads authenticated `player_id` claim |
| `StaminaController.AdLifeReward` | method | Rejects full stamina via service validation |
| `StageController.ReviveAd` | method | Attempt-bound rewarded revive |
| `RewardsController.Claim` | method | Generic source claim, e.g. `DAILY_STAMINA_UNLIMITED` |
| `AdRewardsController.Claim` | method | Generic ad reward claim for supported placements |

## Rules
- Do not accept `user_id` from request bodies; use `ControllerBaseEx.PlayerId`.
- Controllers return contract DTOs only.

## Cross-refs
- Depends on: `ProjectFlood.Application.Stamina.StaminaService`
- Depends on: `ProjectFlood.Application.Stage.StageAttemptService`
- Depends on: `ProjectFlood.Application.Rewards.RewardService`
