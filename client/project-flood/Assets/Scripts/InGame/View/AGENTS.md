# InGame/View - MonoBehaviour Rendering

## Files
| file | class | role |
|------|-------|------|
| `BoardView.cs` | `BoardView` | instantiates/positions CellView grid; Refresh on board change; tap/removal/gravity/rotation visual sequences; ScreenToCell hit-test |
| `CellView.cs` | `CellView` | renders single cell: color, type sprite, protector overlay, core indicator; code-driven interaction/removal/drop effects |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `BoardView._cellPrefab` | SerializeField | CellView prefab to instantiate |
| `BoardView._cellSize` | SerializeField | world-space size per cell (default 1f) |
| `BoardView._tapFeedbackDuration` | SerializeField | tap punch/flash duration |
| `BoardView._groupPulseDuration` | SerializeField | matched group ripple pulse duration |
| `BoardView._removeDuration` | SerializeField | cell pop/fade removal duration |
| `BoardView._protectorHitDuration` | SerializeField | shield-hit shake/flash duration |
| `BoardView._dropDuration` | SerializeField | base gravity fall duration before distance add-on |
| `BoardView._staggerDelay` | SerializeField | Manhattan/column ripple delay for hypercasual feel |
| `BoardView._burstCount` | SerializeField | code-generated burst dots per removed cell |
| `BoardView._colorPalette` | private field | Color[] built at runtime from `ColorPalette.ResourcePath` CSV |
| `BoardView.Build(board,colorIds)` | method | initial setup -> instantiates grid; calls Refresh |
| `BoardView.Refresh(board)` | method | syncs all CellViews to current board state |
| `BoardView.PlayTapFeedback(row,col)` | coroutine | selected cell scale punch + color flash |
| `BoardView.PlayGroupPulse(group,originRow,originCol)` | coroutine | group ripple before resolution |
| `BoardView.PlayRemovalEffects(boardAfterRemoval,group,originRow,originCol)` | coroutine | removed cells pop/burst/fade; protected cells shake/flash |
| `BoardView.PlayGravity(beforeGravity,boardAfterGravity)` | coroutine | animates packed cells from source row to final row with landing squash |
| `BoardView.PlayBoardRotation(quarterTurns)` | coroutine | public visual hook for future board rotation rules |
| `BoardView.ScreenToCell(screenPos)` | method | screen pos -> (row,col); returns (-1,-1) if out of bounds |
| `CellView.SetData(data,color)` | method | null data -> deactivate; else update sprite/color/overlays |
| `CellView.PlayTapFeedback(duration)` | coroutine | code-only tap punch and bright flash |
| `CellView.PlayGroupPulse(delay,duration)` | coroutine | delayed matched-cell pulse |
| `CellView.PlayRemove(duration,burstCount)` | coroutine | pop/shrink/fade and generated burst dots |
| `CellView.PlayProtectorHit(duration)` | coroutine | protector hit shake and flash |
| `CellView.PlayDrop(from,to,delay,duration)` | coroutine | gravity fall with overshoot easing and landing squash |
| `CellView._protectorOverlay` | SerializeField | SpriteRenderer for strength-1 or strength-2 shield |
| `CellView._coreIndicator` | SerializeField | GameObject shown when is_core=true |

## Rules
- View classes are read-only consumers; no game logic
- `_colorPalette` configured in Inspector; index must match color_id from CSV
- ScreenToCell uses `Camera.main`; ensure main camera tag is set in scene

## Cross-refs
- Depends on: `Game.InGame.Board.*`
- Consumed by: `Game.InGame.Controller.InGameController`
