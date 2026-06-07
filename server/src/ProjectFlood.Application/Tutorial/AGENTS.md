# ProjectFlood.Application.Tutorial — Tutorial Service

Namespace: `ProjectFlood.Application.Tutorial`

## Files
| file | class | role |
|------|-------|------|
| `TutorialService.cs` | `TutorialService` | Application service querying and saving tutorial progress to DB |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `TutorialService.GetCompletedTutorialIdsAsync` | method | Returns list of completed tutorial group/step IDs |
| `TutorialService.CompleteTutorialAsync` | method | Saves a new completed tutorial ID to DB |

## Rules
- DB persistence uses injected `AppDbContext`.
- Standard async patterns with CancellationToken.

## Cross-refs
- Consumed by: `ProjectFlood.API.Controllers.TutorialController`
- Depends on: `ProjectFlood.Infrastructure.Generated.AppDbContext`
