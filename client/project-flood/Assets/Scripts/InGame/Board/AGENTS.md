# InGame/Board — Pure C# Data Models

## Files
| file | class | role |
|------|-------|------|
| `CellData.cs` | `CellData` | struct — color_id, cell_type, protector_strength (0–2), is_core |
| `BoardState.cs` | `BoardState` | 2D `CellData?[,]` grid + metadata (width, height, initialValidCells, hasCore) |
| `StarResult.cs` | `StarResult` | enum — Fail=0, Star1=1, Star2=2, Star3=3 |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `CellData.color_id` | field | 0–15; hex C in CTM encoding |
| `CellData.cell_type` | field | `CellType` from `ProjectFlood.Contracts.GameTypes` |
| `CellData.protector_strength` | field | 0=no shield, 1–2=hit count to remove |
| `CellData.is_core` | field | true if bit 2 of M hex is set |
| `BoardState.Grid` | prop | `CellData?[,]` — null slot = empty |
| `BoardState.InitialValidCells` | prop | non-Obstacle count at load time |
| `BoardState.HasCore` | prop | true if any cell has is_core=true |

## Rules
- Zero `UnityEngine` imports — pure C#
- `null` cell = empty slot (removed or out-of-bounds)

## Cross-refs
- Depends on: `ProjectFlood.Contracts.GameTypes.CellType`
- Consumed by: `Game.InGame.Rules.*`, `Game.InGame.Controller.*`, `Game.InGame.View.*`
