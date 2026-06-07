# shared/contracts/Currency

## Files
| file | class | role |
|------|-------|------|
| `CurrencyResponses.cs` | `CurrencySnapshot` | Soft currency balance snapshot DTO |
| `CurrencyRequests.cs` | `SpendSoftRequest` | Spend endpoint request DTO |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `CurrencySnapshot.SoftAmount` | property | Current soft currency balance |
| `SpendSoftRequest.Amount` | property | Amount to deduct |
| `SpendSoftRequest.Reason` | property | Audit log reason (e.g. "continue") |

## Cross-refs
- Consumed by: `ProjectFlood.API.Controllers.AdController`
- Consumed by: `ProjectFlood.Application.Currency.CurrencyService`
