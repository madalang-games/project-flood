# Scripts/InGame — Game Gameplay Domain

Pure C# rule engine + MonoBehaviour view. See ADR-006 for architecture rationale.

## Nav
| path | role |
|------|------|
| `Board/` | Pure C# data models — CellData, BoardState, StarResult | → `Board/AGENTS.md` |
| `Rules/` | Pure C# algorithms — GroupSelector, RemovalSystem, ProtectorSystem, GravitySystem, ClearEvaluator | → `Rules/AGENTS.md` |
| `Items/` | Pure C# item system — ItemType, ItemInventory, ItemManager, Bomb/HRocket/VRocket effects | → `Items/AGENTS.md` |
| `Controller/` | MonoBehaviour orchestrator — InGameController, TurnManager, StageLoader | → `Controller/AGENTS.md` |
| `View/` | MonoBehaviour renderers — BoardView, CellView, ItemTrayView, ItemSlotView | → `View/AGENTS.md` |

## Rules
- `Board/` and `Rules/` must have **zero** `UnityEngine` imports — pure C# only
- `View/` classes are read-only consumers of board state — no game logic
- `InGameController` is the single MonoBehaviour that owns the rule engine
- Namespace: `Game.InGame.[SubDir]` (e.g. `Game.InGame.Rules`)
- NEW_DIR: create `AGENTS.md` for it + update Nav above

## Cross-refs
- Depends on: `shared/contracts/GameTypes/GameEnums.cs` (CellType, Difficulty)
- Depends on: `Scripts/Data/Generated/` (StageRow — generated CSV model)
- Mirrors: `tools/stage_editor/src/lib/game-rules.ts` (TS port — keep in sync)
