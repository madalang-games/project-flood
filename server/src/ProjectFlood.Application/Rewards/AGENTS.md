# ProjectFlood.Application/Rewards

## Files
| file | class | role |
|------|-------|------|
| `RewardService.cs` | `RewardService` | Generic reward source listing, claim handling, and reward group dispatch |
| `AdRewardService.cs` | `AdRewardService` | Generic ad reward facade for supported placements |
| `IAdRewardVerifier.cs` | `IAdRewardVerifier`, `AdVerifyResult` | Provider reward verification boundary |
| `DevelopmentAdRewardVerifier.cs` | `DevelopmentAdRewardVerifier` | Dev verifier — returns Verified=true, ProviderTxId=adToken |
| `AdMobSsvKeyCache.cs` | `AdMobSsvKeyCache` | IHostedService — fetches Google ECDSA public keys hourly |
| `AdMobSsvCallbackService.cs` | `AdMobSsvCallbackService` | Processes AdMob SSV callback: verifies ECDSA, stores Redis ssv:{nonce}=txid |
| `AdMobSsvVerifier.cs` | `AdMobSsvVerifier` | IAdRewardVerifier prod impl — Redis ssv:{nonce} lookup |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `AdVerifyResult` | record | `(bool Verified, string ProviderTxId)` |
| `IAdRewardVerifier.VerifyAsync` | method | `(provider, adToken, ct)` → `AdVerifyResult`; adToken = SSV nonce |
| `RewardService.ClaimAsync` | method | Claims `reward_source` and dispatches reward items |
| `RewardService.GrantRewardGroupAsync` | method | Dispatches reward group items (SOFT_CURRENCY, STAMINA_UNLIMITED) |
| `AdRewardService.ClaimAsync` | method | Routes `STAMINA_LIFE` ad reward through stamina service |
| `AdMobSsvKeyCache.GetKeyBytes` | method | Returns ECDSA public key bytes by keyId |
| `AdMobSsvCallbackService.ProcessAsync` | method | rawQuery → verify ECDSA → Redis ssv:{nonce}=txid TTL 5min |

## Rules
- Daily claim periods use KST.
- Stamina unlimited remains a generic reward item, not a stamina-specific endpoint.
- Dev environment: `DevelopmentAdRewardVerifier` — no SSV required.
- Prod environment: `AdMobSsvVerifier` — nonce must exist in Redis.
- `VerifyAsync` consumes the nonce (GETDEL); non-200 responses from SSV callback trigger Google retry.

## Cross-refs
- Depends on: `shared/datas/reward/reward_source.csv`
- Depends on: `shared/datas/reward/reward_item.csv`
- Consumed by: `ProjectFlood.API.Controllers.RewardsController`
- Consumed by: `ProjectFlood.Application.Stamina.StaminaService`
- Consumed by: `ProjectFlood.Application.Stage.StageAttemptService`
- Consumed by: `ProjectFlood.Application.Stage.AdDoubleRewardService`
