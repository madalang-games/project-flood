# In-Game & Core Gameplay Rules Checklist

Checklist for the core match-and-collapse gameplay mechanics, rule engine, board gimmicks, and win/fail condition validation.

## 1. Core Match & Gravity Rules (MVP)
- [x] **BFS Same-Color Selection**: Find all 4-directionally adjacent cells matching the tapped cell's color. Diagonal adjacency is ignored. Isolated cells of group size 1 are valid for removal.
  - Reference: [GroupSelector.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/Rules/GroupSelector.cs)
- [x] **Downward Gravity Compaction**: Floating cells fall downward. No horizontal compaction (empty columns remain empty).
  - Reference: [GravitySystem.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/Rules/GravitySystem.cs)
- [x] **Void Boundary Gravity Segmenting**: Void cells act as gravity boundaries. Gravity applies independently within each column segment partitioned by Void boundaries.
  - Reference: [GravitySystem.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/Rules/GravitySystem.cs)

## 2. Grid & Gimmick Cells (MVP)
- [x] **Obstacle Cells**: Excluded from selection and clearance ratio calculation. Can only be destroyed by item effects (Bomb, Rockets).
  - Reference: [CellData.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/Board/CellData.cs)
- [x] **Void Cells**: Board shape boundaries (L-shape, T-shape). Invisible, non-interactive, excluded from clearance ratio denominator.
  - Reference: [CellData.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/Board/CellData.cs)
- [x] **Protector Cells (1-2 Layers)**: Direct-hit stripping rule (stripped by same-color group tap or item applied directly). Decrement strength layer until basic cell is exposed.
  - Reference: [ProtectorSystem.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/Rules/ProtectorSystem.cs)
- [x] **Core Cells**: Ultimate stage gate. Must be completely removed to clear the stage, regardless of clearance ratio.
  - Reference: [CellData.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/Board/CellData.cs)

## 3. Game Loop & Controls (MVP)
- [x] **Turn Consuming**: Normal taps decrement available turns. Items do not decrement turns.
  - Reference: [TurnManager.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/Controller/TurnManager.cs)
- [x] **180° Board Rotation Gimmick**: Rotate board 180° around the center, swap logical grid nodes, and apply gravity. Triggers via developer UI button.
  - Reference: [InGameController.cs:L119](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/Controller/InGameController.cs#L119) and [BoardState.cs:L20](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/Board/BoardState.cs#L20)
- [x] **Win/Fail Evaluation**: Ratio-based evaluation (1 Star = 80%, 2 Stars = 90%, 3 Stars = 100%/Clear All basic cells). Fail if core cells remain.
  - Reference: [ClearEvaluator.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/Rules/ClearEvaluator.cs)
- [x] **Stage End Triggers**: Auto-ends on 3-star (early termination) or turns = 0.
  - Reference: [InGameController.cs:L148](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/Controller/InGameController.cs#L148)

## 4. In-Game & Gimmick Development (Active Scope)
- [ ] **Dynamic Turn-Interval Board Rotation**: Board automatically rotates 180° every N player turns (value read from `rotation_interval` field in stage data).
  - [ ] Implement automatic rotation simulation in Next.js web editor playtest mode (`tools/stage_editor/src/lib/game-rules.ts`).
  - [ ] Support rotation logic in the auto-solver TS search algorithm (`tools/stage_editor/src/lib/solver.ts`).
- [ ] **Interactive Dynamic Board Elements**:
  - [ ] Implement Teleport/Portal cells: Inlet coordinate re-routes falling cells to Outlet coordinate. Modify C# `GravitySystem` and Next.js `game-rules.ts`.
  - [ ] Implement Conveyor Belts: Shift cells resting on belt by 1 cell in path direction at turn end, before gravity.
- [ ] **Automatic Board Solver**: Multi-step AI search algorithm (min-moves solver) integrated into editor export checks to mathematically verify stage solvable status.

## 5. Excluded Scope (Phase 2+)
- [ ] **Color Hide Gimmick**: Cells in specific zones or intervals have their colors hidden (rendered as gray or question marks), requiring players to deduce color matching. (Excluded per user request)
