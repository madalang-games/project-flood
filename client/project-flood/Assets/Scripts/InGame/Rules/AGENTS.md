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
| `GroupSelector.FindGroup(board,row,col)` | method | BFS; size ≥ 1 always (ADR-004) |
| `ProtectorSystem.DirectHit(ref cell)` | method | returns true = remove; false = cell stays with reduced strength |
| `RemovalSystem.Remove(board,group)` | method | mutates board.Grid in-place |
| `GravitySystem.Apply(board)` | method | row 0 = top; cells fall toward higher row index |
| `ClearEvaluator.Evaluate(board,star1,star2)` | method | uses board.InitialValidCells and board.HasCore |

## Rules
- Zero `UnityEngine` imports — pure C#

## Cross-refs
- Depends on: `Game.InGame.Board.*`
- Consumed by: `Game.InGame.Controller.InGameController`
