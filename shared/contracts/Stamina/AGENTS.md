# shared/contracts/Stamina

## Files
| file | class | role |
|------|-------|------|
| `StaminaRequests.cs` | `StaminaAdLifeRewardRequest` | Stamina ad life reward request DTO |
| `StaminaResponses.cs` | `StaminaSnapshot`, `StaminaStatusResponse`, `StaminaAdLifeRewardResponse` | Stamina response DTOs |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `StaminaSnapshot` | DTO | Current life, cap, next recovery, unlimited state |
| `StaminaAdLifeRewardResponse.Duplicate` | property | Duplicate tx returns delta 0 and latest snapshot |

## Rules
- DTOs only; no DB, Redis, or business logic models.

## Cross-refs
- Consumed by: `ProjectFlood.API.Controllers.StaminaController`
- Consumed by: `ProjectFlood.Application.Stamina.StaminaService`
