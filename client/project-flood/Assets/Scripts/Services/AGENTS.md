# Scripts/Services - Client Service Boundaries

Namespace: `Game.Services`

## Files
| file | class | role |
|------|-------|------|
| `StageDataService.cs` | `StageDataService` | DDOL singleton; loads Stage CSV via CsvLoader; GetStage(id), GetAll(), MaxStageId() |
| `PlayerProgressService.cs` | `PlayerProgressService` | DDOL singleton; gold balance, per-stage stars/unlock stored in PlayerPrefs |
| `AuthService.cs` | `AuthService` | DDOL singleton; auth result enum; Initialize(callback); stub until real HTTP auth wiring |
| `LocalizationService.cs` | `LocalizationService` | DDOL singleton; loads string/error CSV tables; Get(key), GetError(code), SetLanguage(Language), GetFont(Language) |
| `IAdService.cs` | `IAdService`, `AdWatchResult` | Ad service interface; WatchRewardedAd(placementId, cb); ShowInterstitialIfEligible(stageId, suppress, cb) |
| `AdMobService.cs` | `AdMobService` | DDOL singleton; implements IAdService; multi-placement rewarded ads + interstitial; SSV nonce set before Show() |
| `AdEligibilityCache.cs` | `AdEligibilityCache` | DDOL singleton; GET /api/ad/eligibility on session start; IsEligible(placementId); OnInterstitialShown() |
| `StageApiService.cs` | `StageApiService` | Optional server stage attempt start/clear/fail sync |
| `RankingApiService.cs` | `RankingApiService` | Optional server ranking page/my-rank fetcher |

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
| `PlayerProgressService.GetBestStars(int)` | method | 0..3; lazy-loaded from PlayerPrefs |
| `PlayerProgressService.IsStageUnlocked(int)` | method | Stage 1 always true; lazy-loaded |
| `PlayerProgressService.RecordClear(int,int)` | method | Updates best stars + unlocks stageId+1 |
| `AuthService.IsGuest` | prop | true until OAuth link |
| `AuthService.UserId` | prop | Device UUID or OAuth ID |
| `AuthService.Initialize(Action<AuthResult>)` | method | Stub: guest by default; fires AuthResult.ReLoginRequired if OAuth was linked |
| `AuthService.LinkOAuth(string)` | method | Sets IsGuest=false, persists OAuth ID |
| `AuthService.Logout()` | method | Clears all auth prefs |
| `AuthResult` | enum | Authenticated / Guest / ReLoginRequired |
| `AdWatchResult.Earned` | field | true if user earned the reward |
| `AdWatchResult.AdToken` | field | SSV nonce; pass to server POST endpoint for reward claim |
| `IAdService.WatchRewardedAd(string,Action<AdWatchResult?>)` | method | null result = cancel/fail/no-ad loaded |
| `IAdService.ShowInterstitialIfEligible(int,bool,Action<bool>)` | method | bool wasShown; caller posts /api/ad/interstitial/shown if true |
| `AdMobService.Instance` | prop | DDOL singleton |
| `AdEligibilityCache.Instance` | prop | DDOL singleton |
| `AdEligibilityCache.Refresh(string,string)` | method | baseUrl, optional authToken; fetches GET /api/ad/eligibility |
| `AdEligibilityCache.IsEligible(string)` | method | Returns false if placement not in cache |
| `AdEligibilityCache.GetCooldownSeconds(string)` | method | Returns 0 if not in cache |
| `AdEligibilityCache.OnInterstitialShown()` | method | Optimistically marks INTERSTITIAL_POST_STAGE ineligible until next Refresh |
| `StageApiService.StartAttempt` | method | POST `/api/stages/{stageId}/attempts/start`; stores current attempt |
| `StageApiService.ClearAttempt` | method | POST clear request with summary validation inputs |
| `StageApiService.FailAttempt` | method | POST fail for current attempt |
| `RankingApiService.FetchGlobalPage` | method | GET paged global ranking |
| `RankingApiService.FetchMyGlobalRank` | method | GET current user's ranking card |
| `RankingApiService.FetchMyStageRank` | method | GET current user's stage rank |

## Rules
- All services are DDOL; place GameObjects in Boot scene only.
- PlayerPrefs keys must not clash: prefix `auth_`, `gold`, `stars_`, `unlocked_`, `lang`.
- AuthService is a stub; server-side auth is Phase 2.
- LocalizationService must initialize before any LocalizedText.Awake(); place it first in Boot scene.
- AdEligibilityCache.Refresh must be called after auth is available.
- StageApiService and RankingApiService are optional until full server auth wiring lands; local flow must continue if absent.
- AdMobService ad unit IDs are all test IDs; replace with production IDs before release.
- AdMobService SDK-missing stub must return `null` for rewarded ads; reward success is verified or mocked only server-side.
- pkt_generator must be run to sync Ad + Currency contracts to Generated/Contracts/ before ad flows work.

## Cross-refs
- Depends on: `Game.Utils.CsvLoader`, `ProjectFlood.Data.Generated.Stage`, `Game.Localization.FontLocalizationConfig`
- Depends on: `GoogleMobileAds.Api` (Google Mobile Ads SDK)
- Consumed by: Boot scene, Lobby scene, InGame scene entry, `Game.Core.UI.LocalizedText`
