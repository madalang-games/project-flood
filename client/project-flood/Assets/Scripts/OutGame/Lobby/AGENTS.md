# OutGame/Lobby - Lobby Scene

Namespace: `Game.OutGame.Lobby`

## Files
| file | class | role |
|------|-------|------|
| `LobbyView.cs` | `LobbyView` | Root lobby controller; shows/hides tabs; refreshes gold |
| `HeaderView.cs` | `HeaderView` | Avatar button account entry + gold display |
| `BottomNavBarView.cs` | `BottomNavBarView` | 3-tab nav; fires OnTabChanged |
| `RankingTabView.cs` | `RankingTabView` | Ranking tab UI; stars/max-stage tabs, my rank, top page text |
| `HomeTabView.cs` | `HomeTabView` | Chapter/stage scroll with object pool; stage node tap -> StageInfoPopup -> InGame |
| `StageNodeView.cs` | `StageNodeView` | Pooled node: stage label, 3 star fills, lock overlay, pulse ring; Bind(id,stars,unlocked,current) |
| `StageInfoPopupView.cs` | `StageInfoPopupView` | Stage info popup: title, best stars, best moves, PLAY button |
| `ScrollStateCache.cs` | `ScrollStateCache` | Static session memory: HomeScrollPosition, LastPlayedStageId |

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
| `LobbyTab` | enum | Home / Shop / Ranking |

## Rules
- Scroll position must be saved in HomeTabView.OnDisable and restored in HomeTabView.OnEnable.
- StageNodeView pool size = GameConfig.StageNodePoolSize (15).
- Ranking tab is active; if `RankingApiService` is absent, show unavailable state without breaking lobby flow.

## Cross-refs
- Depends on: `Game.Core.UIManager`, `Game.Services.StageDataService`, `Game.Services.PlayerProgressService`, `Game.Services.RankingApiService`
- Consumed by: InGame scene (reads ScrollStateCache.LastPlayedStageId)
