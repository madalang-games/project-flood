# ADR-001: Bomb / HRocket / VRocket — Board Cells Removed, Items Only
Date: 2026-06-04
Status: accepted

## Context
Initial game design (§5.2) defined Bomb, Horizontal Rocket, and Vertical Rocket as board-placed gimmick cells that trigger when their same-color group is tapped. During prototype planning, the decision was made to simplify the board cell type space and reduce editor complexity.

## Decision
Bomb, Horizontal Rocket, and Vertical Rocket are **items only** (player inventory, applied manually). They are NOT placeable as board cells in the stage editor.

CellType enum contains only: `Basic`, `Obstacle`. Future gimmick board cells (if any) will be added as new CellType values under a new ADR.

## Consequences
- Stage editor does not need bomb/rocket placement tools in MVP.
- CellType enum stays small; CTM hex encoding T-digit has room for future gimmick cells (0x2–0xF).
- Item effects (bomb area, row/col clear) still chain-react with board state per §5.3.
- Game design §5.2 and §5.3 updated to reflect removal.
