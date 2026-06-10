# InGame/Rules — Pure C# Algorithms

## Files
| file | class | role |
|------|-------|------|
| `GroupSelector.cs` | `GroupSelector` | BFS 4-directional same-color group; returns `List<(int row, int col)>` |
| `ProtectorSystem.cs` | `ProtectorSystem` | DirectHit: decrement protector_strength or signal removal |
| `RemovalSystem.cs` | `RemovalSystem` | Iterates group; delegates to ProtectorSystem; nulls removed cells |
| `GravitySystem.cs` | `GravitySystem` | Per-column downward packing of non-null cells |
| `ClearEvaluator.cs` | `ClearEvaluator` | clearance_ratio + core check → StarResult |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `GroupSelector.FindGroup(board,row,col)` | method | BFS; returns empty if start cell is null/Obstacle/Void |
| `ProtectorSystem.DirectHit(ref cell)` | method | returns true = remove; false = cell stays with reduced strength |
| `RemovalSystem.Remove(board,group,allowObstacleRemoval)` | method | mutates board.Grid in-place; skips Obstacles if allowObstacleRemoval is false |
| `GravitySystem.Apply(board)` | method | row 0 = top; cells fall toward higher row index; Void cells are fixed segment boundaries; Obstacles fall like regular cells |
| `ClearEvaluator.Evaluate(board,star1,star2)` | method | uses board.InitialValidCells and board.HasCore; excludes Void |

## Void / Obstacle handling per rule
| system | Void behavior | Obstacle behavior |
|--------|--------------|-------------------|
| GroupSelector | start=Void → empty; neighbor=Void → skip | start=Obstacle → empty; neighbor=Obstacle → skip |
| GravitySystem | Void cell stays fixed; resets writeRow for column segment above it | Falls downward just like standard cells |
| ClearEvaluator | Void excluded from remaining count (same as Obstacle) | Excluded from remaining count |
| RemovalSystem | Void never in group → never processed | Processed only if allowObstacleRemoval is true |

## Rules
- Zero `UnityEngine` imports — pure C#

## Cross-refs
- Depends on: `Game.InGame.Board.*`
- Consumed by: `Game.InGame.Controller.InGameController`
