# Scripts/Services - Client Service Boundaries

Namespace: `Game.Services`

## Nav
| path | role |
|------|------|
| `Tutorial/` | Client-side tutorial manager and step sequencer | → `Tutorial/AGENTS.md` |

## Files
| file | class | role |
|------|-------|------|
| `StageDataService.cs` | `StageDataService` | DDOL singleton; loads Stage CSV via CsvLoader; GetStage(id), GetAll(), MaxStageId() |
| `PlayerProgressService.cs` | `PlayerProgressService` | DDOL singleton; gold balance, per-stage stars/unlock, booster inventories |
| `AuthService.cs` | `AuthService` | DDOL singleton; auth result enum; Initialize(callback); stub until real HTTP auth wiring |
| `LocalizationService.cs` | `LocalizationService` | DDOL singleton; loads string/error CSV tables; Get(key), GetError(code), SetLanguage(Language), GetFont(Language) |
| `IAdService.cs` | `IAdService`, `AdWatchResult` | Ad service interface; WatchRewardedAd(placementId, cb); ShowInterstitialIfEligible(stageId, suppress, cb) |
| `AdMobService.cs` | `AdMobService` | DDOL singleton; implements IAdService; multi-placement rewarded ads + interstitial; SSV nonce set before Show() |
| `AdEligibilityCache.cs` | `AdEligibilityCache` | DDOL singleton; GET /api/ad/eligibility on session start; IsEligible(placementId); OnInterstitialShown() |
| `NetworkService.cs` | `NetworkService` | DDOL singleton; centralised HTTP client; Get/Post; injects Application.version + authToken headers; configurable log level |
| `StageApiService.cs` | `StageApiService` | Server stage attempt start/clear/fail/revive sync |
| `RankingApiService.cs` | `RankingApiService` | Optional server ranking page/my-rank fetcher |
| `StaminaApiService.cs` | `StaminaApiService` | Server stamina fetch + ad-life claim; caches last snapshot for client-side estimation |
| `CurrencyApiService.cs` | `CurrencyApiService` | Server soft currency fetch + spend; syncs `PlayerProgressService.Gold` on response |
| `InventoryApiService.cs` | `InventoryApiService` | Server-backed items fetch + spend API client |
| `RewardsApiService.cs` | `RewardsApiService` | Server rewards list fetch + claim API client |
| `TutorialApiService.cs` | `TutorialApiService` | Server-backed tutorial progress fetch + complete API client |
| `ErrorResponseJson.cs` | `ErrorResponseJson` | Serializable helper for server error code extraction |
| `PlayerApiService.cs` | `PlayerApiService` | DDOL singleton; `GET /api/player/progress` fetch; deserializes to `PlayerProgressResponse` |
| `NetworkRetryOptions.cs` | `NetworkRetryOptions` | Options class for HTTP retries with exponential backoff, jitter, loading overlay, and toast messages |
| `SoundManager.cs` | `SoundManager` | DDOL singleton; BGM + SFX playback; SfxCatalog-based PlaySfx(SfxId) with pitch/cooldown; volume + mute in PlayerPrefs |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `SoundManager.Instance` | prop | DDOL singleton |
| `SoundManager.PlayBGM(AudioClip)` | method | Plays loopable background music |
| `SoundManager.PlaySFX(AudioClip)` | method | Plays one-shot sound effect via raw AudioClip |
| `SoundManager.PlaySfx(SfxId)` | method | Catalog-based SFX with pitch variation + cooldown; also SfxEventBus subscriber |
| `SoundManager.BGMVolume` | prop | 0..1; persisted to PlayerPrefs |
| `SoundManager.SFXVolume` | prop | 0..1; persisted to PlayerPrefs |
| `SoundManager.BGMMute` | prop | bool; persisted to PlayerPrefs |
| `SoundManager.SFXMute` | prop | bool; blocks all SFX including catalog-based |
| `NetworkService.Instance` | prop | DDOL singleton; lazy-instantiated if not in scene |
| `NetworkService.SetAuthToken(string)` | method | Called by AuthService after login; injects Bearer token into all subsequent requests |
| `NetworkService.Get(string,Action<bool,string>)` | method | HTTP GET; path relative to AppConfig BaseUrl |
| `NetworkService.Post(string,string,Action<bool,string>)` | method | HTTP POST with JSON body |
| `NetworkLogLevel` | enum | None / ErrorOnly / Normal / Verbose — controls log output granularity |
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
| `PlayerProgressService.SetGold(int)` | method | Overwrite gold to server-authoritative value; persists to PlayerPrefs |
| `PlayerProgressService.GetBestStars(int)` | method | 0..3; lazy-loaded from PlayerPrefs |
| `PlayerProgressService.IsStageUnlocked(int)` | method | Stage 1 always true; lazy-loaded |
| `PlayerProgressService.RecordClear(int,int)` | method | Updates best stars + unlocks stageId+1 |
| `AuthService.IsGuest` | prop | true until OAuth link |
| `AuthService.UserId` | prop | Device UUID or OAuth ID |
| `AuthService.PendingBootMessage` | static prop | Optional toast message to show after Boot redirect (currently unused) |
| `AuthService.Initialize(Action<AuthResult>)` | method | Guest by default; fires ReLoginRequired if OAuth refresh fails; fires NewGuestCreated if account switch detected |
| `AuthService.LinkGoogle(idToken,nonce,cb)` | method | `POST /api/auth/link-oauth` with `guestRefreshToken`; cb(ok, err, LinkAccountResponseJson) |
| `AuthService.ResolveConflict(token,selection,cb)` | method | `POST /api/auth/resolve-conflict`; calls CompleteSession with returned auth tokens |
| `AuthService.Logout()` | method | Clears all auth prefs; kept for internal use only — no UI button |
| `AuthResult` | enum | Authenticated / Guest / ReLoginRequired / NewGuestCreated |
| `AuthService.LinkAccountResponseJson` | class | `{success, conflict, localSave, cloudSave, conflictToken}` — public inner class |
| `AuthService.SaveSnapshotJson` | class | `{maxStageId, gold, totalStars, totalItems}` — public inner class |
| `AdWatchResult.Earned` | field | true if user earned the reward |
| `AdWatchResult.AdToken` | field | SSV nonce; pass to server POST endpoint for reward claim |
| `IAdService.WatchRewardedAd(string,Action<AdWatchResult?>)` | method | null result = cancel/fail/no-ad loaded |
| `IAdService.ShowInterstitialIfEligible(int,bool,Action<bool>)` | method | bool wasShown; caller posts /api/ad/interstitial/shown if true |
| `AdMobService.Instance` | prop | DDOL singleton |
| `AdEligibilityCache.Instance` | prop | DDOL singleton |
| `AdEligibilityCache.Refresh()` | method | No-arg overload; uses NetworkService for base URL + auth token |
| `AdEligibilityCache.Refresh(string,string)` | method | Legacy overload; baseUrl, optional authToken |
| `AdEligibilityCache.IsEligible(string)` | method | Returns false if placement not in cache |
| `AdEligibilityCache.GetCooldownSeconds(string)` | method | Returns 0 if not in cache |
| `AdEligibilityCache.OnInterstitialShown()` | method | Optimistically marks INTERSTITIAL_POST_STAGE ineligible until next Refresh |
| `StageApiService.StartAttempt` | method | POST `/api/stages/{stageId}/attempts/start`; stores current attempt |
| `StageApiService.ClearAttempt` | method | POST clear request with summary validation inputs |
| `StageApiService.FailAttempt` | method | POST fail for current attempt |
| `RankingApiService.FetchGlobalPage` | method | GET paged global ranking |
| `RankingApiService.FetchMyGlobalRank` | method | GET current user's ranking card |
| `RankingApiService.FetchMyStageRank` | method | GET current user's stage rank |
| `CurrencyApiService.FetchGold` | method | GET `/api/currency`; calls `PlayerProgressService.SetGold` on success |
| `CurrencyApiService.SpendGold` | method | POST `/api/currency/spend`; deducts server-side; calls `PlayerProgressService.SetGold` on success |
| `InventoryApiService.FetchInventory` | method | GET `/api/inventory`; updates progress cache on success |
| `InventoryApiService.SpendItem` | method | POST `/api/inventory/spend`; deducts server-side item balance |
| `RewardsApiService.FetchRewardSources` | method | GET `/api/rewards/sources`; fetch generic reward milestones status |
| `RewardsApiService.ClaimReward` | method | POST `/api/rewards/claim`; claims milestone reward (gold/boosters) and syncs stamina/inventory |
| `PlayerProgressService.GetItemCount` | method | Returns count of booster itemId |
| `PlayerProgressService.SetItemCount` | method | Sets local count of booster itemId |
| `PlayerProgressService.SetInventory` | method | Updates all item counts in cache from snapshot |
| `PlayerProgressService.LoadFromServer` | method | Overwrites unlock/star cache with server-authoritative `PlayerProgressResponse`; clears stale PlayerPrefs |
| `PlayerApiService.Instance` | prop | DDOL singleton |
| `PlayerApiService.FetchProgress` | method | GET /api/player/progress; callback (bool ok, PlayerProgressResponse) |
| `AuthResult.NewGuestCreated` | enum value | Fired by Initialize() when a new guest account replaced the previous one (PID mismatch detected) |
| `StageApiService.ReviveAd` | method | POST `/api/stages/{stageId}/attempts/{attemptId}/revive-ad` with verification retry polling |
| `StageApiService.CurrentAttempt` | prop | Active attempt metadata including revive limits |
| `NetworkRetryOptions.None` | prop | Preset that disables retries |
| `NetworkRetryOptions.LobbyAndSave` | prop | Preset for important lobby and save requests (3 retries, overlay, and toast error messages) |
| `NetworkService.Get(string,NetworkRetryOptions,Action<bool,string>)` | method | HTTP GET with custom retry options |
| `NetworkService.Post(string,string,NetworkRetryOptions,Action<bool,string>)` | method | HTTP POST with custom retry options |

## Rules
- All services are DDOL; place GameObjects in Boot scene only.
- PlayerPrefs keys must not clash: prefix `auth_`, `gold`, `stars_`, `unlocked_`, `lang`.
- AuthService is a stub; server-side auth is Phase 2.
- LocalizationService must initialize before any LocalizedText.Awake(); place it first in Boot scene.
- AdEligibilityCache.Refresh() must be called after auth is available (token set via NetworkService).
- StageApiService and RankingApiService are optional until full server auth wiring lands; local flow must continue if absent.
- AdMobService ad unit IDs are all test IDs; replace with production IDs before release.
- AdMobService SDK-missing stub must return `null` for rewarded ads; reward success is verified or mocked only server-side.
- pkt_generator must be run to sync Ad + Currency contracts to Generated/Contracts/ before ad flows work.
- **NetworkService owns all HTTP transmission**: do NOT add UnityWebRequest code to individual services.
- NetworkService._enableLogging is forced false when AppEnvironment == Prod.

## Cross-refs
- Depends on: `Game.Utils.CsvLoader`, `ProjectFlood.Data.Generated.Stage`, `Game.Localization.FontLocalizationConfig`
- Depends on: `GoogleMobileAds.Api` (Google Mobile Ads SDK)
- Consumed by: Boot scene, Lobby scene, InGame scene entry, `Game.Core.UI.LocalizedText`
