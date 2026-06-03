# InGame/Controller — MonoBehaviour Orchestration

## Files
| file | class | role |
|------|-------|------|
| `StageLoader.cs` | `StageLoader` | static — parses CTM hex cells string + color_ids → BoardState |
| `InGameSceneEntry.cs` | `InGameSceneEntry` | MonoBehaviour — loads Stage CSV from Resources, calls InGameController.Init |
| `TurnManager.cs` | `TurnManager` | tracks remaining_turns; Consume() returns bool (turns left?) |
| `InGameController.cs` | `InGameController` | MonoBehaviour orchestrator; owns rule engine; drives tap → result flow |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `StageLoader.Load(stage)` | method | Stage → BoardState; computes initialValidCells and hasCore |
| `StageLoader.ParseColorIds(str)` | method | "1,3,5" → int[] |
| `TurnManager.RemainingTurns` | prop | read-only |
| `TurnManager.Consume()` | method | decrements; returns true if turns remain after |
| `InGameController.Init(stage)` | method | entry point; loads board, builds view, sets _isPlaying=true |
| `InGameController.OnStageEnd` | event | `Action<StarResult, int>` — (result, remainingTurns) |
| `InGameController.OnTurnConsumed` | event | `Action<int>` — remainingTurns after consume |

## Rules
- `InGameController` is the single MonoBehaviour owning the rule engine
- Input: polls `Mouse.current` and `Touchscreen.current` in Update (New Input System)
- Stage end triggers when `result == Star3` OR `turns == 0`

## Cross-refs
- Depends on: `Game.InGame.Board.*`, `Game.InGame.Rules.*`, `Game.InGame.View.BoardView`
- Depends on: `ProjectFlood.Data.Generated.Stage`
