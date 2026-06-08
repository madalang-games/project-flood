# InGame Core Design

## Architecture

Pure C# rule engine + MonoBehaviour view layer. See ADR-006 for rationale.

```
Input (New Input System)
  → InGameController (MonoBehaviour)
      ├── StageLoader        reads CSV → BoardState
      ├── GroupSelector      BFS same-color group
      ├── RemovalSystem      remove group + protector handling
      ├── GravitySystem      downward column packing
      ├── TurnManager        turn tracking
      └── ClearEvaluator     ratio + core + star result
            ↓
      BoardView / CellView (MonoBehaviour, read-only from board state)
```

Rule engine classes have **zero UnityEngine dependency**.

---

## Module Breakdown

### Board/ (pure C# data)

| class | role |
|-------|------|
| `CellType` | enum — Basic=0, Obstacle=1 (mirrors `GameEnums.cs`) |
| `CellData` | struct — color_id, CellType, protector_strength (0–2), is_core |
| `BoardState` | 2D `CellData?[,]` grid + `initial_valid_cells` count |

`null` cell = empty slot (post-removal before gravity, or out-of-bounds).

### Rules/ (pure C# algorithms)

| class | role |
|-------|------|
| `GroupSelector` | BFS from tap position; returns `List<(int row, int col)>`. Size ≥ 1 always valid (ADR-004). |
| `RemovalSystem` | Iterates group; calls ProtectorSystem per cell; removes cells with protector_strength=0 |
| `ProtectorSystem` | Direct-hit logic: decrement protector_strength; expose underlying cell if 0 (ADR-002) |
| `GravitySystem` | Per-column: compact non-null cells downward; fill top with null |
| `ClearEvaluator` | Computes clearance_ratio; checks core cleared; returns `StarResult` |

### Controller/ (MonoBehaviour layer)

| class | role |
|-------|------|
| `StageLoader` | Parses CTM hex `cells` string → `BoardState`; parses `color_ids` |
| `TurnManager` | Tracks `remaining_turns`; `Consume()` returns bool (turns left?) |
| `InGameController` | MonoBehaviour orchestrator; owns rule engine instances; drives tap → result flow |

### View/ (MonoBehaviour, rendering only)

| class | role |
|-------|------|
| `BoardView` | Instantiates/positions `CellView` grid; updates on board change |
| `CellView` | Renders single cell: color, type sprite, protector overlay, core indicator |

---

## Tap Flow

```
1. Player taps screen position
2. InGameController → hit test → (row, col)
3. GroupSelector.FindGroup(board, row, col)
   → BFS 4-directional same-color adjacent cells
   → returns List<(row,col)>, size ≥ 1
4. RemovalSystem.Remove(board, group)
   → for each cell in group:
       ProtectorSystem.DirectHit(cell)
         protector_strength > 0 → strength--   (cell stays)
         protector_strength = 0 → board[r,c] = null  (cell removed)
5. GravitySystem.Apply(board)
   → each column: non-null cells pack downward
6. TurnManager.Consume()
   → remaining_turns--
7. ClearEvaluator.Evaluate(board, initialValidCells, hasCoreFlag)
   → clearance_ratio = (initialValidCells - remaining_valid) / initialValidCells
   → core_cleared = no is_core cell in remaining cells
   → StarResult: Fail / Star1 / Star2 / Star3
8. InGameController handles StarResult
   → Star3 or turns=0 → stage end
   → else → await next tap
```

---

## Clear Conditions

```
initial_valid_cells = total board cells − Obstacle cells   (computed at stage load)
remaining_valid     = non-null, non-Obstacle cells on board at evaluation

clearance_ratio = (initial_valid_cells − remaining_valid) / initial_valid_cells

WIN  = clearance_ratio ≥ star1_ratio AND core_cleared
FAIL = clearance_ratio < star1_ratio OR NOT core_cleared

Stars:
  3 = all valid cells removed (remaining_valid = 0)  ← early termination
  2 = clearance_ratio ≥ star2_ratio
  1 = clearance_ratio ≥ star1_ratio
```

---

## Stage Loading

`StageLoader` input: `StageRow` (generated C# model from `stage.csv`).

```
cells string  → split into 3-char chunks (CTM hex)
chunk index i → row = i / board_width, col = i % board_width
C hex char    → color_id (0–15)
T hex char    → CellType (0=Basic, 1=Obstacle)
M hex char    → protector_strength = M & 0x3, is_core = (M & 0x4) != 0
```

`color_ids` string → parsed as comma-separated ints → available color set for UI highlight.

---

## Namespace Convention

```
Game.InGame.Board      → CellData, BoardState
Game.InGame.Rules      → GroupSelector, RemovalSystem, ProtectorSystem, GravitySystem, ClearEvaluator
Game.InGame.Controller → InGameController, TurnManager, StageLoader
Game.InGame.View       → BoardView, CellView
```

---

## Portal & Conveyor Mechanics

### Portal (Teleport Cell) Gravity Resolution
- When resolving gravity, `GravitySystem` first builds column chains.
- For column columns containing Portals (Inlet and Outlet):
  - The Inlet cell acts as the top of its column segment, and the Outlet cell acts as the bottom of the column segment receiving from it.
  - Vacancy at or below the Outlet pulls cells from above the Inlet:
    `board[outletRow, outletCol] = board[inletRow - 1, inletCol]`
  - Falling animation is tracked by `BoardView.PlayGravity` using a multi-phase translation pathway instead of a simple linear vertical path.

### Conveyor Belt Shift Resolution
- Happens inside `InGameController.HandleTapSequence` (and after items) **before** `GravitySystem.Apply()`.
- Runs conveyor paths:
  - Collects all cells lying on a conveyor path in order.
  - Shifts cell data arrays by 1 step in the path direction.
  - The tail of the conveyor is filled by the cell sliding off the adjacent slot, and the head slides onto the next.
  - Overhangs created by conveyor shifts are subsequently resolved by `GravitySystem.Apply()`.

