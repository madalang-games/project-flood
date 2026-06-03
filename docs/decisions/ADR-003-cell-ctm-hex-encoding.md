# ADR-003: Stage Cell Encoding — CTM Hex Format
Date: 2026-06-04
Status: accepted

## Context
Stage cells in `stage.csv` must encode color, cell type, and modifiers (protector strength, core flag) in a flat serialized string. Options considered:

- Decimal CCTF (4–5 digits/cell): readable but grows with new modifiers; bakes combos into T digit.
- JSON array per cell: extensible but verbose (~1 KB overhead/cell).
- CTM hex (3 hex chars/cell, no separator): compact, extensible, fixed-width parsing.

## Decision
Each cell is encoded as 3 hex characters `CTM` (no separator between cells). The `cells` column is a flat row-major string; board is read as `cells[i*3..i*3+3]`.

```
C = color_id hex (0–F, maps to color_palette.color_id)
T = CellType hex  (0=Basic, 1=Obstacle; 2–F reserved for future gimmick cells)
M = modifier hex  (bitmask)
    bits [1:0] = protector_strength (0=none, 1=1-layer, 2=2-layer)
    bit  [2]   = is_core (0/1)
    bit  [3]   = reserved
```

Examples:
- `500` — color5 Basic, no modifier
- `501` — color5 Basic, Protector 1-layer
- `506` — color5 Basic, Core + Protector 2-layer
- `010` — Obstacle (color digit is 0 by convention, unused)

Max board 16×16 = 256 cells × 3 chars = **768 chars** flat.

## Consequences
- Fixed-width parsing: `i*3` offset, no split/tokenize needed.
- T digit extensible to 15 future CellTypes without re-encoding existing stages.
- M bitmask extensible to bit[3] for a future modifier.
- Hex requires editor/client to render values as hex strings; straightforward in both C# and JS.
- Human readability lower than decimal, but stage data is editor-generated, not hand-written.
