# ProjectFlood.API/Controllers

## Files
| file | class | role |
|------|-------|------|
| `ControllerBaseEx.cs` | `ControllerBaseEx` | Shared internal user id and correlation id helpers |
| `StaminaController.cs` | `StaminaController` | `/api/stamina` status and ad life reward endpoints |
| `StageController.cs` | `StageController` | Stage attempt start/clear/fail/revive endpoints |
| `RewardsController.cs` | `RewardsController` | Generic reward source listing and claim endpoints |
| `AdRewardsController.cs` | `AdRewardsController` | Generic rewarded-ad claim endpoint |
| `AdSsvCallbackController.cs` | `AdSsvCallbackController` | `[AllowAnonymous]` GET `/api/ad/ssv-callback` — AdMob SSV callback |
| `AdController.cs` | `AdController` | `/api/ad` eligibility, interstitial shown, double reward claim |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `ControllerBaseEx.PlayerId` | property | Reads internal `user_id` claim resolved from JWT `sub` |
| `StaminaController.AdLifeReward` | method | Rejects full stamina via service validation |
| `StageController.Start` | method | Uses `stage_start` rate-limit policy |
| `StageController.ReviveAd` | method | Attempt-bound rewarded revive |
| `RewardsController.Claim` | method | Generic source claim, e.g. `DAILY_STAMINA_UNLIMITED` |
| `AdRewardsController.Claim` | method | Generic ad reward claim for supported placements |
| `AdSsvCallbackController.SsvCallback` | method | `GET /api/ad/ssv-callback` — always returns 200 |
| `AdController.GetEligibility` | method | `GET /api/ad/eligibility` — interstitial cooldown state |
| `AdController.InterstitialShown` | method | `POST /api/ad/interstitial/shown` — records shown timestamp |
| `AdController.ClaimDoubleReward` | method | `POST /api/ad/double-reward/claim` — 2x stage clear reward |

## Rules
- Do not accept `user_id` from request bodies; use `ControllerBaseEx.PlayerId`.
- Do not parse JWT `sub` as a numeric id; it is platform PID.
- Controllers return contract DTOs only.

## Cross-refs
- Depends on: `ProjectFlood.Application.Stamina.StaminaService`
- Depends on: `ProjectFlood.Application.Stage.StageAttemptService`
- Depends on: `ProjectFlood.Application.Rewards.RewardService`
