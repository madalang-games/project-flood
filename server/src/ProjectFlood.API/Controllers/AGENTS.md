# ProjectFlood.API/Controllers

## Files
| file | class | role |
|------|-------|------|
| `ControllerBaseEx.cs` | `ControllerBaseEx` | Shared internal user id and correlation id helpers |
| `StaminaController.cs` | `StaminaController` | `/api/stamina` status and ad life reward endpoints |
| `StageController.cs` | `StageController` | Stage attempt start/clear/fail/revive endpoints |
| `RankingController.cs` | `RankingController` | Global and stage ranking endpoints |
| `RewardsController.cs` | `RewardsController` | Generic reward source listing and claim endpoints |
| `AdRewardsController.cs` | `AdRewardsController` | Generic rewarded-ad claim endpoint |
| `AdSsvCallbackController.cs` | `AdSsvCallbackController` | `[AllowAnonymous]` GET `/api/ad/ssv-callback` — AdMob SSV callback |
| `AdController.cs` | `AdController` | `/api/ad` eligibility, interstitial shown, double reward claim |
| `AuthController.cs` | `AuthController` | `/api/auth` proxy endpoints for guest, google, refresh, logout |
| `BootstrapController.cs` | `BootstrapController` | `/api/bootstrap` configurations and schema/meta hash checks |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `ControllerBaseEx.PlayerId` | property | Reads internal `user_id` claim resolved from JWT `sub` |
| `StaminaController.AdLifeReward` | method | Rejects full stamina via service validation |
| `StageController.Start` | method | Uses `stage_start` rate-limit policy |
| `StageController.Clear` | method | Accepts server validation summary for clear/ranking calculation |
| `StageController.ReviveAd` | method | Attempt-bound rewarded revive |
| `RankingController.GetGlobal` | method | Paged `/api/rankings/global/{type}` list |
| `RankingController.GetMyGlobal` | method | Current user's global rank card |
| `RankingController.GetMyStageRank` | method | Current user's stage best-turn rank |
| `RankingController.Rebuild` | method | `POST /api/rankings/admin/rebuild`; auth-gated Redis rebuild trigger |
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
- Depends on: `ProjectFlood.Application.Ranking.RankingService`
- Depends on: `ProjectFlood.Application.Rewards.RewardService`
