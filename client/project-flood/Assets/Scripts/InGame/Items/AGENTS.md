# InGame/Items — Pure C# Item System

## Files
| file | class | role |
|------|-------|------|
| `ItemType.cs` | `ItemType` | enum — Bomb, HRocket, ColorSweep, RowShift, CellSwap |
| `ItemInventory.cs` | `ItemInventory` | count store + dev mode toggle; CanUse / Consume / GetCount |
| `IItemEffect.cs` | `IItemEffect` | interface — GetAffectedCells(board, row, col) |
| `BombEffect.cs` | `BombEffect` | 3×3 blast (9 cells inc. center); Void skipped |
| `HRocketEffect.cs` | `HRocketEffect` | row sweep L→R; Void skip+continue; Obstacle destroy+stop |
| `ColorSweepEffect.cs` | `ColorSweepEffect` | clears all cells matching tapped cell's color_id; Obstacle/Void excluded |
| `RowShiftEffect.cs` | `RowShiftEffect` | packs cells per row toward swipe direction; Void = hard boundary; Apply(board,dir) mutates board directly |
| `CellSwapEffect.cs` | `CellSwapEffect` | validates target cell is non-null, non-Void; swap logic in ItemManager |
| `ItemManager.cs` | `ItemManager` | orchestrates use phase state; owns inventory + effect instances |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `ItemInventory.IsDevMode` | field | Inspector-set bool; if true, items never decrement and always CanUse |
| `ItemInventory.CanUse(type)` | method | true if IsDevMode OR count > 0 |
| `ItemInventory.Consume(type)` | method | no-op when IsDevMode |
| `ItemManager.IsInUsePhase` | prop | true while player has selected an item awaiting target tap |
| `ItemManager.SelectedItem` | prop | nullable ItemType; null when not in phase |
| `ItemManager.FirstSelectedCell` | prop | nullable (row,col); set during CellSwap first-tap flow |
| `ItemManager.OnUsePhaseChanged` | event | `Action<ItemType?>` — null=exited, non-null=entered |
| `ItemManager.SelectItem(type)` | method | enters phase; toggles off if same type re-tapped; guards CanUse |
| `ItemManager.Cancel()` | method | exits phase without consuming item |
| `ItemManager.UseItem(board,row,col,out completed)` | method | returns affected cells; completed=false on CellSwap first tap (wait for second); consumes item and exits phase on completion |
| `ItemManager.UseRowShift(board,direction)` | method | calls RowShiftEffect.Apply, consumes item, exits phase |
| `ItemManager.SetFirstSelectedCell(row,col)` | method | no-op unless SelectedItem==CellSwap |
| `RowShiftEffect.Apply(board,direction)` | method | mutates board in-place; not via GetAffectedCells |
| `ShiftDirection` | enum | Left / Right; defined in RowShiftEffect.cs |
| `IItemEffect.GetAffectedCells(board,row,col)` | method | returns ordered `List<(int row,int col)>` per item rules |

## Rules
- Zero `UnityEngine` imports — pure C#
- `GetAffectedCells` never removes cells; caller (InGameController) drives RemovalSystem
- Void positions always excluded from returned cell list
- Obstacle included in returned list (destroyed by item); Rocket stops after Obstacle entry
- `RowShiftEffect.GetAffectedCells` returns empty list — RowShift uses `Apply()` path, not tap-target path
- `CellSwapEffect.GetAffectedCells` validates cell only; ItemManager performs the actual grid swap
- CellSwap: first tap sets FirstSelectedCell (completed=false); second tap executes swap (completed=true); tapping same cell again cancels selection

## Cross-refs
- Depends on: `Game.InGame.Board.BoardState`, `ProjectFlood.Contracts.GameTypes.CellType`
- Consumed by: `Game.InGame.Controller.InGameController`
- View layer: `Game.InGame.View.ItemTrayView`, `Game.InGame.View.ItemSlotView`
