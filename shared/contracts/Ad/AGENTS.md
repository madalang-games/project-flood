# shared/contracts/Ad

## Files
| file | class | role |
|------|-------|------|
| `AdRequests.cs` | `AdDoubleRewardRequest`, `AdInterstitialShownRequest` | Ad reward and interstitial request DTOs |
| `AdResponses.cs` | `AdPlacementStatus`, `AdEligibilityResponse`, `AdDoubleRewardGrantResponse`, `AdInterstitialShownResponse` | Ad eligibility and reward response DTOs |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `AdDoubleRewardRequest.AdToken` | property | SSV nonce set via `SetServerSideVerificationOptions.CustomData` |
| `AdDoubleRewardRequest.StageId` | property | Stage ID to look up `double_reward_eligible` Redis key |
| `AdDoubleRewardRequest.AttemptId` | property | Attempt ID to validate against Redis eligibility key |
| `AdDoubleRewardGrantResponse.InterstitialSuppressed` | property | true → client should skip next interstitial |
| `AdPlacementStatus.CooldownRemainingSeconds` | property | 0 if eligible |

## Cross-refs
- Consumed by: `ProjectFlood.API.Controllers.AdController`
- Consumed by: `ProjectFlood.Application.Stage.AdDoubleRewardService`
- Consumed by: `ProjectFlood.Application.Stage.AdInterstitialService`
