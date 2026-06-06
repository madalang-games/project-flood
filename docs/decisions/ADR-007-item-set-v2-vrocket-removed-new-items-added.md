# ADR-007: Item Set v2 — VRocket Removed; ColorSweep, RowShift, CellSwap Added
Date: 2026-06-06
Status: accepted

## Context
VRocket (vertical column sweep) was part of the original MVP item set (ADR-001). The game's gravity is vertical-only (cells fall downward). A vertical sweep removes cells that would naturally group together after removal, resulting in an item that feels counterproductive and interrupts expected board flow. Play-testing confirmed VRocket provides negative strategic value in this game type.

Additionally, the "clear all cells" goal identified a gap: isolated cells of the same color that are not spatially adjacent cannot be targeted efficiently by existing items. Three new items address distinct strategic gaps.

## Decision
1. **VRocket removed** from the item set.
2. **ColorSweep added:** removes all cells on the board matching the color of the tapped cell (board-wide, not flood-fill). Targets the endgame scenario where same-color cells are isolated in scattered positions.
3. **RowShift added:** one-time action that packs all cells in each row toward the swipe direction (left or right). Triggered by a horizontal swipe gesture with a minimum distance threshold to prevent accidental activation. Creates adjacency between cells separated by empty slots.
4. **CellSwap added:** swaps the positions of two tapped cells. Two-tap interaction. Simple UX; intended to be given in relatively high quantities.

Item set is now: **Bomb, H-Rocket, ColorSweep, RowShift, CellSwap** (5 items).

## Consequences
- RowShift requires a distinct interaction mode in ItemManager (swipe, not tap).
- CellSwap requires a two-tap selection state in ItemManager.
- item-system-design.md updated to reflect new definitions, UX flows, and data model.
- game-design.md §8 and §13 updated.
- ADR-001 title and body updated to exclude VRocket.
