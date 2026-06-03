# InGame/View — MonoBehaviour Rendering

## Files
| file | class | role |
|------|-------|------|
| `BoardView.cs` | `BoardView` | instantiates/positions CellView grid; Refresh on board change; ScreenToCell hit-test |
| `CellView.cs` | `CellView` | renders single cell: color, type sprite, protector overlay, core indicator |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `BoardView._cellPrefab` | SerializeField | CellView prefab to instantiate |
| `BoardView._cellSize` | SerializeField | world-space size per cell (default 1f) |
| `BoardView._colorPalette` | private field | Color[] built at runtime from `ColorPalette.ResourcePath` CSV |
| `BoardView.Build(board,colorIds)` | method | initial setup — instantiates grid; calls Refresh |
| `BoardView.Refresh(board)` | method | syncs all CellViews to current board state |
| `BoardView.ScreenToCell(screenPos)` | method | screen pos → (row,col); returns (-1,-1) if out of bounds |
| `CellView.SetData(data,color)` | method | null data → deactivate; else update sprite/color/overlays |
| `CellView._protectorOverlay` | SerializeField | SpriteRenderer for strength-1 or strength-2 shield |
| `CellView._coreIndicator` | SerializeField | GameObject shown when is_core=true |

## Rules
- View classes are read-only consumers — no game logic
- `_colorPalette` configured in Inspector; index must match color_id from CSV
- ScreenToCell uses `Camera.main` — ensure main camera tag is set in scene

## Cross-refs
- Depends on: `Game.InGame.Board.*`
- Consumed by: `Game.InGame.Controller.InGameController`
