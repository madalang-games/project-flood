# ADR-004: Isolated Cell (Size=1 Group) Tap Allowed
Date: 2026-06-04
Status: accepted

## Context
Original rule (§4.1): group size = 1 is an invalid tap; turn not consumed. This creates a permanent stuck state: a cell isolated by gravity or obstacles can never be removed. For Core cells, isolation means guaranteed FAIL with no recovery.

## Decision
All taps are valid regardless of group size. BFS returns the cell and its same-color connected group (minimum size 1). Turn is consumed. Cell(s) are removed following normal removal rules (Protector stripping, then cell removal).

## Consequences
- No permanently stuck cells. Isolated non-core cells can always be cleared.
- Isolated Core cells can always be removed; guaranteed FAIL state eliminated.
- Stage editor isolation heuristics still useful as difficulty hints, not blockers.
- `GroupSelector` returns group of size ≥ 1; caller no longer needs size guard.
- game-design.md §4.1 updated.
