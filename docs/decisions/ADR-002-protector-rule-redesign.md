# ADR-002: Protector Cell Rule Redesign
Date: 2026-06-04
Status: accepted

## Context
Original design (§5.2): Protector absorbs 1 hit from any source including adjacent cell removal. Protector could be stacked on any cell type.

Two problems:
1. Adjacent-removal stripping causes unintuitive cascade behavior and complicates gameplay reading.
2. Stacking on any cell type (Core, Obstacle, gimmick cells) increases rule complexity with no clear design benefit for MVP.

## Decision
- Protector is placed **on Basic cells only**.
- Protector inherits the same color as the underlying Basic cell (participates in same-color BFS).
- Strength: 1 or 2 layers (editor-defined). MVP caps at 2.
- **Reaction: direct hit only** — a protector layer is stripped only by:
  - The cell's own same-color group being tapped (this cell is in the matched group), OR
  - An item applied directly to this cell.
- Adjacent cell removal does **not** strip protector.
- Core + Protector stacking remains valid (Core cell with protector shield).

## Consequences
- Player can predict protector behavior: only direct targeting strips it.
- Simpler chain resolution: no need to check protector stripping during gravity or adjacent removal.
- Strength cap of 2 keeps difficulty manageable; cell encoding handles it without combinatorial enum explosion (see ADR-003).
