# OutGame/Lobby - Lobby Scene

Namespace: `Game.OutGame.Lobby`

## Files
| file | class | role |
|------|-------|------|
| `LobbyView.cs` | `LobbyView` | Root lobby controller; shows/hides tabs; refreshes gold |
| `HeaderView.cs` | `HeaderView` | Avatar tap → AccountPopup; stamina display (count/MAX/timer); stamina tap → StaminaPopupView |
| `BottomNavBarView.cs` | `BottomNavBarView` | 3-tab nav; fires OnTabChanged |
| `RankingTabView.cs` | `RankingTabView` | Ranking tab UI; stars/max-stage tabs, my rank, top page text |
| `HomeTabView.cs` | `HomeTabView` | Chapter/stage scroll with object pool; milestone chest rendering + claim API |
| `ChapterChestView.cs` | `ChapterChestView` | Milestone chest node displaying Locked (INACTIVE), Claim! (ACTIVE), or Cleared (CLAIMED) states |
| `StageNodeView.cs` | `StageNodeView` | Pooled node: stage label, 3 star fills, lock overlay, pulse ring; Bind(id,stars,unlocked,current) |
| `StageInfoPopupView.cs` | `StageInfoPopupView` | Stage info popup: title, best stars, best moves, +3 turns toggle (checked against AddTurns inventory), PLAY button |
| `StaminaPopupView.cs` | `StaminaPopupView` | Stamina popup: large heart + count, timer/MAX, Watch Ad (+1) button (dimmed at MAX), backdrop close |
| `ScrollStateCache.cs` | `ScrollStateCache` | Static session memory: HomeScrollPosition, LastPlayedStageId, UseExtraTurnsItem |
| `ShopTabView.cs` | `ShopTabView` | Shop tab UI; showcases coming-soon IAP preview packages (Starter, Item Bundles, No-Ads) |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `LobbyView._header` | SerializeField | HeaderView ref |
| `LobbyView._rankingTabRoot` | SerializeField | Ranking tab root toggled by nav |
| `LobbyView._rankingTabView` | SerializeField | Ranking tab refresh target |
| `BottomNavBarView.OnTabChanged` | event | `Action<LobbyTab>` |
| `BottomNavBarView.SelectTab(LobbyTab)` | method | Public; sets highlight |
| `RankingTabView.Refresh` | method | Fetches page + my rank via `RankingApiService` |
| `HomeTabView` | component | OnEnable refreshes pool; OnDisable saves scroll position |
| `StageNodeView.Bind(id,stars,unlocked,isCurrent)` | method | Updates all visual states |
| `StageNodeView.OnTapped` | event | `Action<int>` stageId |
| `StageInfoPopupView.Init(stageId,bestStars,bestMoves,onPlay)` | method | Required before showing |
| `ScrollStateCache.HomeScrollPosition` | prop | Float 0..1; save on leave, restore on enter |
| `ScrollStateCache.LastPlayedStageId` | prop | Set before entering InGame scene |
| `HeaderView._staminaButton` | SerializeField | Button on StaminaPanel; tapped → StaminaPopupView |
| `HeaderView._staminaText` | SerializeField | TMP text showing current count on heart icon |
| `HeaderView._staminaTimerText` | SerializeField | TMP text showing MAX or HH:MM countdown |
| `StaminaPopupView._watchAdButtonGroup` | SerializeField | CanvasGroup; alpha=0.35 + non-interactable when at MAX |
| `StaminaPopupView.Refresh()` | method | Subscribes to `StaminaApiService.OnStaminaUpdated`; dimming logic |
| `LobbyTab` | enum | Home / Shop / Ranking |
| `ShopTabView` | component | Shop screen preview containing Starter Pack, Item Bundle, and No-Ads package preview buttons |
| `ScrollStateCache.UseExtraTurnsItem` | prop | boolean; true if +3 turns booster is active for next attempt |
| `HomeTabView.CreateChestNode` | method | Instantiates a ChapterChestView prefab near the chapter-end stage node |
| `HomeTabView.OnChestTapped` | method | Invokes generic reward claim API, grants Gold/booster items, shows toast, and refreshes UI |
| `ChapterChestView.SetState(ChestState)` | method | Configures sprites, text labels, button interactability, and glow overlays |

## Rules
- Scroll position must be saved in HomeTabView.OnDisable and restored in HomeTabView.OnEnable.
- StageNodeView pool size = GameConfig.StageNodePoolSize (15).
- Ranking tab is active; if `RankingApiService` is absent, show unavailable state without breaking lobby flow.

## Cross-refs
- Depends on: `Game.Core.UIManager`, `Game.Services.StageDataService`, `Game.Services.PlayerProgressService`, `Game.Services.RankingApiService`, `Game.Services.StaminaApiService`
- Consumed by: InGame scene (reads ScrollStateCache.LastPlayedStageId)
