# Scripts/Services — Client Service Boundaries

Namespace: `Game.Services`

## Files
| file | class | role |
|------|-------|------|
| `StageDataService.cs` | `StageDataService` | DDOL singleton; loads Stage CSV via CsvLoader; GetStage(id), GetAll(), MaxStageId() |
| `PlayerProgressService.cs` | `PlayerProgressService` | DDOL singleton; gold balance, per-stage stars/unlock stored in PlayerPrefs |
| `AuthService.cs` | `AuthService` | DDOL singleton; auth result enum; Initialize(callback); stub — Phase 2 adds real HTTP |
| `LocalizationService.cs` | `LocalizationService` | DDOL singleton; loads string/error CSV tables; Get(key), GetError(code), SetLanguage(Language), GetFont(Language) |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `LocalizationService.Instance` | prop | DDOL singleton |
| `LocalizationService.CurrentLanguage` | prop | Active Language enum value |
| `LocalizationService.OnLanguageChanged` | event | Fired after SetLanguage(); LocalizedText subscribes |
| `LocalizationService.Get(string)` | method | Returns localized string for key; falls back to EN then key itself |
| `LocalizationService.GetError(string)` | method | Returns localized error message for server errorCode |
| `LocalizationService.SetLanguage(Language)` | method | Reloads tables + fires OnLanguageChanged; saves to PlayerPrefs |
| `LocalizationService.GetFont(Language)` | method | Returns TMP_FontAsset from FontLocalizationConfig; null if config missing |
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
- All services are DDOL — place GameObjects in Boot scene only
- PlayerPrefs keys must not clash: prefix `auth_`, `gold`, `stars_`, `unlocked_`, `lang`
- AuthService is a stub; server-side auth is Phase 2
- LocalizationService must initialize before any LocalizedText.Awake() — place it first in Boot scene

## Cross-refs
- Depends on: `Game.Utils.CsvLoader`, `ProjectFlood.Data.Generated.Stage`, `Game.Localization.FontLocalizationConfig`
- Consumed by: Boot scene, Lobby scene, InGame scene entry, `Game.Core.UI.LocalizedText`
