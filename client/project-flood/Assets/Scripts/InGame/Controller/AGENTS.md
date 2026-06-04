# InGame/Controller - MonoBehaviour Orchestration

## Files
| file | class | role |
|------|-------|------|
| `StageLoader.cs` | `StageLoader` | static; parses CTM hex cells string + color_ids -> BoardState |
| `InGameSceneEntry.cs` | `InGameSceneEntry` | MonoBehaviour; loads Stage CSV from Resources, calls InGameController.Init |
| `TurnManager.cs` | `TurnManager` | tracks remaining_turns; Consume() returns bool (turns left?) |
| `InGameController.cs` | `InGameController` | MonoBehaviour orchestrator; owns rule engine; drives tap -> animation -> result flow |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `StageLoader.Load(stage)` | method | Stage -> BoardState; computes initialValidCells and hasCore |
| `StageLoader.ParseColorIds(str)` | method | "1,3,5" -> int[] |
| `TurnManager.RemainingTurns` | prop | read-only |
| `TurnManager.Consume()` | method | decrements; returns true if turns remain after |
| `InGameController.Init(stage)` | method | entry point; loads board, builds view, sets play/animation state, creates ItemManager |
| `InGameController.TriggerRotateBoard()` | method | public; guard-checked entry point for 180-degree board rotation; cancels active UsePhase |
| `InGameController.RotateBoardSequence()` | coroutine | PlayBoardRotation(2) -> BoardState.Rotate180() -> CompleteBoardRotation() -> GravitySystem.Apply() -> PlayGravity() |
| `InGameController.HandleTap(row,col)` | method | finds group and starts animated tap sequence |
| `InGameController.HandleTapSequence(row,col,group)` | coroutine | tap feedback -> group pulse -> removal effects -> gravity drop -> turn/result |
| `InGameController.HandleItemTap(row,col)` | method | UseItem -> starts HandleItemSequence if cells non-empty |
| `InGameController.HandleItemSequence(originRow,originCol,cells)` | coroutine | removal effects -> gravity drop -> clear eval (no turn consumed) |
| `InGameController.OnItemSlotTapped(type)` | method | guard (_isPlaying, _isAnimating) then SelectItem |
| `InGameController.OnItemUsePhaseChanged(selected)` | method | SetItemTargetMode on BoardView; Refresh ItemTrayView |
| `InGameController.CloneGrid(board)` | method | snapshots board grid before gravity animation |
| `InGameController.OnStageEnd` | event | `Action<StarResult, int>` -> (result, remainingTurns) |
| `InGameController.OnTurnConsumed` | event | `Action<int>` -> remainingTurns after consume |
| `InGameController._isDevMode` | SerializeField | passed to ItemInventory.IsDevMode on Init |
| `InGameController._itemTrayView` | SerializeField | optional; null = no item UI |

## Rules
- `InGameController` is the single MonoBehaviour owning the rule engine
- Input: polls `Mouse.current` and `Touchscreen.current` in Update (New Input System)
- Stage end triggers when `result == Star3` OR `turns == 0`
- Ignore new input while `_isAnimating=true` to keep visual state deterministic

## Cross-refs
- Depends on: `Game.InGame.Board.*`, `Game.InGame.Rules.*`, `Game.InGame.View.BoardView`
- Depends on: `ProjectFlood.Data.Generated.Stage`
