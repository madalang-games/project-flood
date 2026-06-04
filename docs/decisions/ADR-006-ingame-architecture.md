# ADR-006: InGame Architecture — Pure C# Rule Engine + MonoBehaviour View
Date: 2026-06-04
Status: accepted

## Context
Unity MonoBehaviour classes are tightly coupled to the engine lifecycle, making game logic hard to unit-test and reason about independently.

## Decision
Strict separation between rule engine and view:

- **Rule engine** (`InGame/Board/`, `InGame/Rules/`) — pure C# classes, zero Unity dependency (`UnityEngine` not imported). Contains all game state and algorithms.
- **View** (`InGame/View/`) — MonoBehaviour classes only. Read rule engine state; no game logic.
- **Controller** (`InGame/Controller/`) — MonoBehaviour orchestrator. Owns the rule engine instance. Bridges input → rule engine → view updates.

```
Input (New Input System)
  → InGameController (MB)
    → GroupSelector / RemovalSystem / GravitySystem / ClearEvaluator
      → BoardState (pure data)
    → BoardView / CellView (MB) ← state read-only
```

## Consequences
- Rule engine is independently testable without Unity.
- View classes stay thin; no state duplication.
- `InGameController` is the single point of Unity coupling for game flow.
- TypeScript port in `tools/stage_editor/src/lib/game-rules.ts` mirrors the rule engine logic — changes to rules require updating both.
