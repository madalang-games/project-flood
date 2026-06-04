# shared/contracts/Rewards

## Files
| file | class | role |
|------|-------|------|
| `RewardRequests.cs` | `RewardClaimRequest`, `AdRewardClaimRequest` | Reward claim request DTOs |
| `RewardResponses.cs` | `RewardSourceDto`, `GrantedRewardDto`, `RewardClaimResponse`, `AdRewardClaimResponse` | Reward source and claim response DTOs |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `RewardClaimRequest.SourceId` | property | Generic claim source, e.g. `DAILY_STAMINA_UNLIMITED` |
| `GrantedRewardDto.RewardType` | property | Generic reward type, e.g. `STAMINA_UNLIMITED` |

## Rules
- Keep claim source generic; do not add stamina-specific daily unlimited request DTOs.

## Cross-refs
- Consumed by: `ProjectFlood.API.Controllers.RewardsController`
- Consumed by: `ProjectFlood.Application.Rewards.RewardService`
