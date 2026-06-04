# Scripts/Services — Client Service Boundaries

Namespace: `Game.Services`

## Files
| file | class | role |
|------|-------|------|
| `StageDataService.cs` | `StageDataService` | DDOL singleton; loads Stage CSV via CsvLoader; GetStage(id), GetAll(), MaxStageId() |
| `PlayerProgressService.cs` | `PlayerProgressService` | DDOL singleton; gold balance, per-stage stars/unlock stored in PlayerPrefs |
| `AuthService.cs` | `AuthService` | DDOL singleton; auth result enum; Initialize(callback); stub — Phase 2 adds real HTTP |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `StageDataService.GetStage(int)` | method | Returns Stage or null |
| `StageDataService.GetAll()` | method | Returns Stage[] |
| `PlayerProgressService.Gold` | prop | Current gold balance |
| `PlayerProgressService.CanAfford(int)` | method | Gold >= cost check |
| `PlayerProgressService.SpendGold(int)` | method | Returns false if insufficient gold |
| `PlayerProgressService.AddGold(int)` | method | Persists to PlayerPrefs |
| `PlayerProgressService.GetBestStars(int)` | method | 0–3; lazy-loaded from PlayerPrefs |
| `PlayerProgressService.IsStageUnlocked(int)` | method | Stage 1 always true; lazy-loaded |
| `PlayerProgressService.RecordClear(int,int)` | method | Updates best stars + unlocks stageId+1 |
| `AuthService.IsGuest` | prop | true until OAuth link |
| `AuthService.UserId` | prop | Device UUID or OAuth ID |
| `AuthService.Initialize(Action<AuthResult>)` | method | Stub: guest by default; fires AuthResult.ReLoginRequired if OAuth was linked |
| `AuthService.LinkOAuth(string)` | method | Sets IsGuest=false, persists OAuth ID |
| `AuthService.Logout()` | method | Clears all auth prefs |
| `AuthResult` | enum | Authenticated / Guest / ReLoginRequired |

## Rules
- All three services are DDOL — place GameObjects in Boot scene only
- PlayerPrefs keys must not clash: prefix `auth_`, `gold`, `stars_`, `unlocked_`
- AuthService is a stub; server-side auth is Phase 2

## Cross-refs
- Depends on: `Game.Utils.CsvLoader`, `ProjectFlood.Data.Generated.Stage`
- Consumed by: Boot scene, Lobby scene, InGame scene entry
