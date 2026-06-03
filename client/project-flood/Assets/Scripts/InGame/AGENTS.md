# Scripts/InGame — Game Gameplay Domain

Pure C# rule engine + MonoBehaviour view. See ADR-006 for architecture rationale.

## Nav
| path | role |
|------|------|
| `Board/` | Pure C# data models — CellData, BoardState |
| `Rules/` | Pure C# algorithms — GroupSelector, RemovalSystem, ProtectorSystem, GravitySystem, ClearEvaluator |
| `Controller/` | MonoBehaviour orchestrator — InGameController, TurnManager, StageLoader |
| `View/` | MonoBehaviour renderers — BoardView, CellView |

## Rules
- `Board/` and `Rules/` must have **zero** `UnityEngine` imports — pure C# only
- `View/` classes are read-only consumers of board state — no game logic
- `InGameController` is the single MonoBehaviour that owns the rule engine
- Namespace: `Game.InGame.[SubDir]` (e.g. `Game.InGame.Rules`)
- NEW_DIR: create `AGENTS.md` for it + update Nav above

## Cross-refs
- Depends on: `shared/contracts/GameTypes/GameEnums.cs` (CellType, Difficulty)
- Depends on: `Scripts/Data/Generated/` (StageRow — generated CSV model)
- Mirrors: `stage-editor/src/lib/game-rules.ts` (TS port — keep in sync)
