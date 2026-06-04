# ProjectFlood.Application/Stamina

## Files
| file | class | role |
|------|-------|------|
| `StaminaConfigProvider.cs` | `StaminaConfigProvider`, `StaminaRuntimeConfig` | Loads stamina config from generated CSV data |
| `StaminaService.cs` | `StaminaService` | Life, regen, ad life reward, and unlimited state transitions |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `StaminaConfigProvider.Current` | property | Runtime config source; no hardcoded attempt TTL |
| `StaminaService.GrantAdLifeAsync` | method | Unlimited daily ad life claims, capped at max life |
| `StaminaService.SpendForAttemptAsync` | method | Attempt start life spend; skips during unlimited |
| `StaminaService.GrantUnlimitedAsync` | method | EXTEND stack policy for stamina unlimited |

## Rules
- All time comparisons use UTC except KST daily reset calculations in reward services.
- Mutation responses must include a full `StaminaSnapshot`.

## Cross-refs
- Depends on: `shared/datas/stamina/stamina_config.csv`
- Depends on: `ProjectFlood.Infrastructure.Generated.UserStaminaStateRow`
- Consumed by: `ProjectFlood.Application.Stage.StageAttemptService`
