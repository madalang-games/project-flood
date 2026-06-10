# InGame/View - MonoBehaviour Rendering

## Files
| file | class | role |
|------|-------|------|
| `BoardView.cs` | `BoardView` | instantiates/positions CellView grid; Refresh on board change; tap/removal/gravity/rotation visual sequences; ScreenToCell hit-test; drives BoardBackground |
| `CellView.cs` | `CellView` | renders single cell: color, type sprite, protector overlay, core indicator, target highlight; code-driven interaction/removal/drop effects |
| `BoardBackground.cs` | `BoardBackground` | procedural dynamic neon pixel-art board panel + per-cell socket sprites + scene-visible Void cutouts |
| `InGameSceneBackgroundView.cs` | `InGameSceneBackgroundView` | World Space SR background for InGame; Night palette; sparkle particles at ~8 FPS; Bind(bgThemeId) |
| `DevRotateButton.cs` | `DevRotateButton` | UNITY_EDITOR or DEVELOPMENT_BUILD only; wires an existing UI Button or creates a fallback button for InGameController.TriggerRotateBoard |
| `ItemSlotView.cs` | `ItemSlotView` | single item slot — count badge (TMP), Button, selected highlight; Refresh(count,devMode,canUse,selected) |
| `ItemTrayView.cs` | `ItemTrayView` | HorizontalLayoutGroup container for 3 ItemSlotViews; fires OnSlotTapped event; SetLocked for animation lock |
| `HUDView.cs` | `HUDView` | Canvas_Scene: Pause button, TurnCounter (Icon+TMP), ProgressContainer (Horizontal: CellIcon+RemainingText+Star1/2/3 images); Init(totalTurns,initialValidCells,star1Ratio,star2Ratio) |
| `ResultOverlayView.cs` | `ResultOverlayView` | Canvas_Overlay: stage result — star pop sequence, stats, Retry/Next/Map buttons |
| `FailOverlayView.cs` | `FailOverlayView` | Canvas_Overlay: continue popup — continue cost, owned gold, Continue/Forfeit; disables button if gold < cost |
| `PausePopupView.cs` | `PausePopupView` | Canvas_Overlay: pause menu — Resume/Restart/Settings/StageSelect buttons |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `HUDView._turnsText` | SerializeField | TMP for remaining turns; label-free — icon carries semantic context |
| `HUDView._remainingText` | SerializeField | TMP for remaining cell count |
| `HUDView._starImages` | SerializeField | Image[3]: star1/2/3; star_empty default, star_filled applied left→right by ratio |
| `HUDView._starFilled` | SerializeField | Sprite applied when star threshold is reached |
| `HUDView._starEmpty` | SerializeField | Sprite applied when threshold not yet met |
| `HUDView._turnsBorder` | SerializeField | Image on TurnsBubble/Border; color driven by remaining turns |
| `HUDView._safeColor` | SerializeField | Border color when turns > 10 (neon green) |
| `HUDView._cautionColor` | SerializeField | Border color at 5 turns (golden yellow) |
| `HUDView._dangerColor` | SerializeField | Border base color at ≤3 turns (neon red) |
| `HUDView._dangerPulseColor` | SerializeField | Border pulse peak color for danger neon effect |
| `HUDView._pulseDuration` | SerializeField | Seconds per half-cycle of danger pulse (default 0.8) |
| `HUDView.Init(totalTurns,initialValidCells,star1Ratio,star2Ratio)` | method | stores thresholds; calls UpdateTurns + UpdateRemainingCells |
| `HUDView.UpdateTurns(remaining)` | method | sets _turnsText; calls RefreshBorderColor |
| `HUDView.UpdateRemainingCells(remaining)` | method | sets _remainingText; calls RefreshStars |
| `HUDView.RefreshStars(remaining)` | method | computes ratio → 0/1/2/3 filled stars |
| `HUDView.RefreshBorderColor(remaining)` | method | gradient: >10 safe, 5-10 safe↔caution, 3-5 caution↔danger, ≤3 starts pulse |
| `HUDView.PulseBorder()` | coroutine | neon pulse — PingPong Lerp between _dangerColor and _dangerPulseColor |
| `BoardView._cellPrefab` | SerializeField | CellView prefab to instantiate |
| `BoardView._background` | SerializeField | BoardBackground reference; parented/aligned to BoardView at Build |
| `BoardView._boardScreenRatio` | SerializeField | target camera viewport fill used for computed cell size |
| `BoardView._tapFeedbackDuration` | SerializeField | tap punch/flash duration |
| `BoardView._groupPulseDuration` | SerializeField | matched group ripple pulse duration |
| `BoardView._removeDuration` | SerializeField | cell pop/fade removal duration |
| `BoardView._protectorHitDuration` | SerializeField | shield-hit shake/flash duration |
| `BoardView._dropDuration` | SerializeField | base gravity fall duration before distance add-on |
| `BoardView._staggerDelay` | SerializeField | Manhattan/column ripple delay for hypercasual feel |
| `BoardView._burstCount` | SerializeField | code-generated burst dots per removed cell |
| `BoardView._rotateDuration` | SerializeField | 180-degree visual board rotation duration |
| `BoardView._rotateScalePulse` | SerializeField | subtle board scale pulse during rotation |
| `BoardView.Build(board,colorIds)` | method | initial setup -> computes cell positions, instantiates grid, aligns background, calls Refresh |
| `BoardView.Refresh(board)` | method | syncs all CellViews + socket visibility to current board state |
| `BoardView.PlayTapFeedback(row,col)` | coroutine | selected cell scale punch + color flash |
| `BoardView.PlayGroupPulse(group,originRow,originCol)` | coroutine | group ripple before resolution |
| `BoardView.PlayRemovalEffects(boardAfterRemoval,group,originRow,originCol)` | coroutine | removed cells pop/burst/fade; protected cells shake/flash |
| `BoardView.PlayGravity(beforeGravity,boardAfterGravity)` | coroutine | animates non-Void packed cells per Void-delimited segment with landing squash |
| `BoardView.AnimateGravitySegment(beforeGravity,boardAfterGravity,col,topRow,bottomRow,maxDelay)` | method | maps fall animation source rows inside one gravity segment |
| `BoardView.PlayBoardRotation(quarterTurns)` | coroutine | visual parent Transform rotation with ease/scale pulse |
| `BoardView.CompleteBoardRotation(board)` | method | resets BoardView transform to upright identity and refreshes rotated board data |
| `BoardView.ScreenToCell(screenPos)` | method | screen pos -> (row,col); returns (-1,-1) if out of bounds |
| `BoardView.SetItemTargetMode(active)` | method | sets/clears target highlight on all valid (non-null, non-Void) cells |
| `CellView._targetHighlight` | SerializeField | optional GameObject; shown when cell is a valid item target |
| `CellView.SetTargetHighlight(active)` | method | shows/hides `_targetHighlight`; null-safe |
| `ItemSlotView.Refresh(count,isDevMode,canUse,selected)` | method | updates badge text, button interactable, selected highlight |
| `ItemTrayView.OnSlotTapped` | event | `Action<ItemType>` — fired on slot button click |
| `ItemTrayView.Refresh(manager)` | method | syncs all 3 slots to ItemManager state |
| `ItemTrayView.SetLocked(locked)` | method | stores lock flag; next Refresh disables buttons when locked |
| `CellView.Init(cellSize)` | method | sets _baseScale from sprite bounds; sprite defines visual padding |
| `CellView.SetData(data,color)` | method | null or Void data -> deactivate; else update sprite/color/overlays |
| `CellView.PlayTapFeedback(duration)` | coroutine | code-only tap punch and bright flash |
| `CellView.PlayGroupPulse(delay,duration)` | coroutine | delayed matched-cell pulse |
| `CellView.PlayRemove(duration,burstCount)` | coroutine | pop/shrink/fade and generated burst dots |
| `CellView.PlayProtectorHit(duration)` | coroutine | protector hit shake and flash |
| `CellView.PlayDrop(from,to,delay,duration)` | coroutine | gravity fall with overshoot easing and landing squash |
| `CellView._protectorOverlay` | SerializeField | SpriteRenderer for strength-1 or strength-2 shield |
| `CellView._coreIndicator` | SerializeField | GameObject shown when is_core=true |
| `InGameSceneBackgroundView.Bind(bgThemeId)` | method | resolves Night palette, builds 1×16 gradient SR at sortOrder -100, builds sparkle SRs at -99 |
| `BoardBackground.Build(width,height,cellSize,cellPositions)` | method | generates dynamic panel + socket sprites using BoardView's exact cell positions |
| `BoardBackground.Refresh(width,height,showSocket,showHole)` | method | enables sockets and stores Void map for transparent panel cutouts |
| `BoardBackground.Update()` | method | refreshes neon pixel-art panel texture, Void-adjacent rim, and socket tint at low FPS |
| `DevRotateButton._controller` | SerializeField | InGameController ref; auto-finds via FindObjectOfType if null |

## Rules
- View classes are read-only consumers; no game logic
- ScreenToCell uses `Camera.main`; ensure main camera tag is set in scene
- BoardBackground may be on the same GameObject or assigned as a sibling; BoardView aligns it on Build
- DevRotateButton compiled only in UNITY_EDITOR or DEVELOPMENT_BUILD
- Board rotation is 180 degrees only (quarterTurns=2); 90/270 are not supported

## Cross-refs
- Depends on: `Game.InGame.Board.*`, `Game.Core.UI.*`
- Consumed by: `Game.InGame.Controller.InGameController`, `Game.InGame.Controller.InGameSceneEntry`
