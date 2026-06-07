# ProjectFlood.Application/Currency

## Files
| file | class | role |
|------|-------|------|
| `CurrencyService.cs` | `CurrencyService` | Soft currency balance read, grant, and spend |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `CurrencyService.GrantSoftAsync` | method | Adds amount to `user_currency.soft_amount`, logs CurrencyChanged event |
| `CurrencyService.SpendSoftAsync` | method | Deducts amount; throws `InsufficientCurrency` if balance insufficient; logs CurrencyChanged event |
| `CurrencyService.GetAsync` | method | Returns `CurrencySnapshot` (0 if no row) |

## Rules
- `user_currency` row created lazily on first grant.
- All mutations go through `GrantSoftAsync` or `SpendSoftAsync` for consistent event logging.
- Spend throws `GameApiException(InsufficientCurrency)` — never floors to 0.

## Cross-refs
- Depends on: `ProjectFlood.Infrastructure.Generated.UserCurrencyRow`
- Consumed by: `ProjectFlood.Application.Rewards.RewardService`
