# InGame/View - MonoBehaviour Rendering

## Files
| file | class | role |
|------|-------|------|
| `BoardView.cs` | `BoardView` | instantiates/positions CellView grid; Refresh on board change; tap/removal/gravity/rotation visual sequences; ScreenToCell hit-test; drives BoardBackground |
| `CellView.cs` | `CellView` | renders single cell: color, type sprite, protector overlay, core indicator; code-driven interaction/removal/drop effects |
| `BoardBackground.cs` | `BoardBackground` | procedural dynamic neon pixel-art board panel + per-cell socket sprites |
| `DevRotateButton.cs` | `DevRotateButton` | UNITY_EDITOR or DEVELOPMENT_BUILD only; wires an existing UI Button or creates a fallback button for InGameController.TriggerRotateBoard |

## Symbols
| symbol | kind | note |
|--------|------|------|
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
| `BoardView.PlayGravity(beforeGravity,boardAfterGravity)` | coroutine | animates non-Void packed cells from source row to final row with landing squash |
| `BoardView.PlayBoardRotation(quarterTurns)` | coroutine | visual parent Transform rotation with ease/scale pulse |
| `BoardView.CompleteBoardRotation(board)` | method | resets BoardView transform to upright identity and refreshes rotated board data |
| `BoardView.ScreenToCell(screenPos)` | method | screen pos -> (row,col); returns (-1,-1) if out of bounds |
| `CellView.Init(cellSize)` | method | sets _baseScale from sprite bounds; sprite defines visual padding |
| `CellView.SetData(data,color)` | method | null or Void data -> deactivate; else update sprite/color/overlays |
| `CellView.PlayTapFeedback(duration)` | coroutine | code-only tap punch and bright flash |
| `CellView.PlayGroupPulse(delay,duration)` | coroutine | delayed matched-cell pulse |
| `CellView.PlayRemove(duration,burstCount)` | coroutine | pop/shrink/fade and generated burst dots |
| `CellView.PlayProtectorHit(duration)` | coroutine | protector hit shake and flash |
| `CellView.PlayDrop(from,to,delay,duration)` | coroutine | gravity fall with overshoot easing and landing squash |
| `CellView._protectorOverlay` | SerializeField | SpriteRenderer for strength-1 or strength-2 shield |
| `CellView._coreIndicator` | SerializeField | GameObject shown when is_core=true |
| `BoardBackground.Build(width,height,cellSize,cellPositions)` | method | generates dynamic panel + socket sprites using BoardView's exact cell positions |
| `BoardBackground.Refresh(width,height,showSocket)` | method | enables/disables socket SpriteRenderers per empty-cell map |
| `BoardBackground.Update()` | method | refreshes neon pixel-art panel texture and socket tint at low FPS |
| `DevRotateButton._controller` | SerializeField | InGameController ref; auto-finds via FindObjectOfType if null |

## Rules
- View classes are read-only consumers; no game logic
- ScreenToCell uses `Camera.main`; ensure main camera tag is set in scene
- BoardBackground may be on the same GameObject or assigned as a sibling; BoardView aligns it on Build
- DevRotateButton compiled only in UNITY_EDITOR or DEVELOPMENT_BUILD
- Board rotation is 180 degrees only (quarterTurns=2); 90/270 are not supported

## Cross-refs
- Depends on: `Game.InGame.Board.*`
- Consumed by: `Game.InGame.Controller.InGameController`
