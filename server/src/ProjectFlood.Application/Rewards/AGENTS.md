# ProjectFlood.Application/Rewards

## Files
| file | class | role |
|------|-------|------|
| `RewardService.cs` | `RewardService` | Generic reward source listing and claim handling |
| `AdRewardService.cs` | `AdRewardService` | Generic ad reward facade for supported placements |
| `IAdRewardVerifier.cs` | `IAdRewardVerifier` | Provider reward verification boundary |
| `DevelopmentAdRewardVerifier.cs` | `DevelopmentAdRewardVerifier` | Local verifier requiring non-empty ad fields |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `RewardService.ClaimAsync` | method | Claims `reward_source` and grants `STAMINA_UNLIMITED` |
| `RewardService.GetSourcesAsync` | method | Returns claimable source state |
| `AdRewardService.ClaimAsync` | method | Routes `STAMINA_LIFE` ad reward through stamina service |
| `IAdRewardVerifier.VerifyAsync` | method | S2S/provider verification hook |

## Rules
- Daily claim periods use KST.
- Stamina unlimited remains a generic reward item, not a stamina-specific endpoint.

## Cross-refs
- Depends on: `shared/datas/reward/reward_source.csv`
- Depends on: `shared/datas/reward/reward_item.csv`
- Consumed by: `ProjectFlood.API.Controllers.RewardsController`
