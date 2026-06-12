# Conveyor Belt Design

Date: 2026-06-12
Status: planning

## Goal

Add a conveyor belt board gimmick that moves cells horizontally at the end of a player action. The gimmick is a board-floor feature, not a removable cell target. It must work in Unity gameplay, the stage editor, playtest simulation, auto generation, solver validation, and 180-degree board rotation.

## Final Rules

| topic | rule |
|------|------|
| Supported directions | `ConveyorLeft`, `ConveyorRight` only |
| Unsupported directions | `ConveyorUp`, `ConveyorDown` are out of scope |
| Placement | A conveyor occupies valid floor slots in one row segment |
| Segment boundary | Board edge or `Void` cell |
| Minimum length | A conveyor segment must cover 2 or more non-void slots |
| Segment direction | One segment has exactly one direction |
| Multiple segments | A row may contain multiple conveyor segments; each segment may have its own direction |
| Cell movement | Move the whole segment by 1 slot in the conveyor direction |
| Wrap | The cell pushed past the segment boundary reappears at the opposite end of the same segment |
| Obstacles | Current `Obstacle` cells move with the conveyor |
| Void | `Void` cells are boundaries and never part of a conveyor segment |
| Gravity | Gravity applies even on conveyor floor slots |
| Server authority | Client board state is authoritative for this gimmick |

Conveyors should not be encoded as `CellType` occupants. The board already starts with every non-void slot filled by a cell, and conveyor must coexist under a `Basic`, special booster, protector, core, or `Obstacle` occupant. Treat conveyor as floor metadata, similar to sockets/background, while `Void` remains an occupant-like boundary in CTM.

## Turn Order

Player tap or item action uses the normal flow:

```text
1. Player interaction removes or changes cells.
2. Existing gravity stabilization completes.
3. All conveyor segments move simultaneously by 1 slot.
4. Gravity stabilization runs again.
5. Clear/fail/turn UI updates run from the stabilized board.
6. If rotation_interval triggers, board rotation runs after the action sequence.
```

Notes:
- This is not a match-3 game. There is no automatic match detection after conveyor movement.
- Conveyor movement may create empty slots only because cells can fall after the conveyor shift; gravity resolves those slots immediately.
- Item flows must be audited. Current item comments say `CellSwap` skips gravity, but conveyors are an end-of-action board gimmick; implementation must explicitly decide whether `CellSwap` triggers conveyors and post-conveyor gravity.

## Segment Model

A conveyor segment is a contiguous run on one row:

```text
row, start_col, end_col, direction
```

Recommended serialized form for `stage.csv` `conveyor_data`:

```text
row:start_col-end_col:L;row:start_col-end_col:R
```

Example:

```text
2:1-5:R;4:0-3:L;4:5-7:R
```

Meaning:
- Row 2, columns 1 through 5 move right.
- Row 4 has two independent conveyor segments split by a void or gap.
- The format is row-segment based and directly exposes direction, which makes validation and 180-degree rotation simpler than path-order strings.

Existing code currently has path-style conveyor handling in C# and TypeScript (`r,c->r,c`). That code should be treated as a prototype to review, not as final spec, because it does not clearly model row-only direction, segment validation, or direction reversal on rotation.

## Movement Semantics

All segments are resolved from the board snapshot before conveyor movement.

For `ConveyorRight`:

```text
[A][B][C][D] -> [D][A][B][C]
```

For `ConveyorLeft`:

```text
[A][B][C][D] -> [B][C][D][A]
```

`null` slots, if present during simulation, move exactly like cells because the segment is slot-based. This keeps playtest and runtime deterministic.

## Validation

Stage editor export/generate/playtest should block invalid conveyor data.

| check | failure |
|------|---------|
| Segment length < 2 | Error |
| Segment includes a `Void` cell | Error |
| Segment coordinates out of board bounds | Error |
| Segment is not horizontal | Error |
| Segments overlap | Error |
| Same continuous non-void conveyor segment mixes L/R | Error |
| Segment does not terminate at board edge or `Void` boundary | Error |

Boundary rule:
- A valid segment starts either at column `0` or immediately after a `Void`.
- A valid segment ends either at `board_width - 1` or immediately before a `Void`.

This makes designer intent explicit and prevents hidden partial-row belts.

## 180-Degree Rotation

When the board rotates 180 degrees:

```text
new_row       = height - 1 - old_row
new_start_col = width - 1 - old_end_col
new_end_col   = width - 1 - old_start_col
direction     = Left <-> Right
```

The rotation transform must be applied in all places that simulate or mutate board state:
- Unity `BoardState.Rotate180()` and surrounding controller flow.
- Stage editor `rotate180`.
- Playtest validation.
- Solver and generator simulation.
- Any saved or recorded replay validation that includes rotation.

## Stage Editor Requirements

The editor must let users place `Void` and conveyor floor metadata before requesting generation. Generate uses that existing board state as fixed constraints.

Required editor behavior:
- Add conveyor brush/mode separate from `CellType`.
- Show `ConveyorLeft` and `ConveyorRight` on floor slots below the occupant.
- Let users erase conveyor without erasing the cell occupant.
- Preserve existing non-void filled-cell invariant during generation.
- Validate conveyor segments before save/export/generate/playtest.
- Auto-generation must not place generated occupants in `Void` slots.
- Auto-generation must simulate conveyor and post-conveyor gravity when scoring candidates.
- Playtest, solver, and validation must share the same conveyor rule implementation or be covered by parity tests.

## Unity Runtime Requirements

Recommended structure:
- Keep occupant data in `CellData`.
- Add a floor metadata structure to `BoardState`, such as parsed conveyor segments.
- Add a dedicated conveyor rule system instead of keeping movement in `InGameController`.
- Make the rule system return movement records so `BoardView` can animate normal slides and wrap slides.
- Extend `BoardBackground` or add a floor overlay layer to render belt sprites below cells.

Runtime order must be audited because current `ShiftConveyors()` appears before gravity in several flows. The final spec requires conveyor after the first gravity stabilization, then gravity again.

## Visual Direction

Do not solve readability by adding global cell spacing in the first implementation. Current assets and board layout use tight 128px-style pixel art squares with beveled edges, transparent corners, and socket/background layers behind cells. Changing board spacing globally would alter tap feel, animations, and board density.

Preferred visual stack:

```text
Board panel
Socket/floor layer
Conveyor belt floor sprite or fallback highlight
Cell occupant
Protector/core/selection overlays
```

Fallback when sprites are missing:
- Draw a low-alpha row highlight on conveyor slots.
- Draw a small left/right arrow marker on each conveyor floor slot or at the segment edges.
- Keep it below cell occupants where possible, but add board-edge arrows if cells cover the floor too much.

Dynamic resource keys:

| key | path | category | note |
|-----|------|----------|------|
| `conveyor_left` | `Assets/Sprites/Gameplay/Conveyors/conveyor_left.png` | `Conveyor` | Floor tile moving left |
| `conveyor_right` | `Assets/Sprites/Gameplay/Conveyors/conveyor_right.png` | `Conveyor` | Floor tile moving right |
| `conveyor_marker_left` | `Assets/Sprites/Gameplay/Conveyors/conveyor_marker_left.png` | `Conveyor` | Optional row-edge UX marker |
| `conveyor_marker_right` | `Assets/Sprites/Gameplay/Conveyors/conveyor_marker_right.png` | `Conveyor` | Optional row-edge UX marker |

If these rows are absent from `shared/datas/common/dynamic_resource.csv`, runtime and editor should use fallback rendering.

## Asset Style Notes

Observed local assets:
- Gameplay cells are square pixel-art sprites, roughly 128x128, with rounded/chamfered transparent corners.
- Cells use high-contrast bevels, top-left highlights, bottom/right shadows, and saturated color fills.
- Sockets are dark, subtle, beveled tiles used as background/floor affordances.
- Item icons use bold white pictograms over saturated beveled square bases.

Conveyor sprites should look closer to socket/floor assets than item icons. They must remain readable under colorful cells without competing with the cell identity.

## Image Generation Prompts

Use these prompts for bitmap asset generation. Export transparent PNGs, then import into Unity with the same pixel-art settings as existing gameplay sprites.

### Conveyor Floor Tile Left

```text
128x128 pixel art game tile, transparent background, dark charcoal conveyor belt floor socket, beveled square tile with chamfered transparent corners, subtle metallic rim, cyan blue arrow chevrons pointing left across the center, low contrast enough to sit under colorful puzzle cells, crisp 1 pixel edges, top-left highlight, bottom-right shadow, no text, no UI frame, no drop shadow outside the tile
```

### Conveyor Floor Tile Right

```text
128x128 pixel art game tile, transparent background, dark charcoal conveyor belt floor socket, beveled square tile with chamfered transparent corners, subtle metallic rim, cyan blue arrow chevrons pointing right across the center, low contrast enough to sit under colorful puzzle cells, crisp 1 pixel edges, top-left highlight, bottom-right shadow, no text, no UI frame, no drop shadow outside the tile
```

### Conveyor Edge Marker Left

```text
128x128 pixel art board-edge marker, transparent background, compact glowing cyan arrow pointing left, dark metal outline, readable at small size, matches hyper-casual puzzle UI, crisp beveled pixels, no text, no square button background, no drop shadow outside the sprite
```

### Conveyor Edge Marker Right

```text
128x128 pixel art board-edge marker, transparent background, compact glowing cyan arrow pointing right, dark metal outline, readable at small size, matches hyper-casual puzzle UI, crisp beveled pixels, no text, no square button background, no drop shadow outside the sprite
```

## Risks

| risk | mitigation |
|------|------------|
| Existing path-style conveyor code conflicts with row-segment spec | Replace or adapt behind a parser with tests |
| Conveyor hidden under cells | Use floor tile plus row-edge markers/fallback highlight |
| Turn order differs between Unity and stage editor | Add parity cases for tap, item, conveyor, gravity, rotation |
| Rotation forgets direction reversal | Centralize segment transform and cover with tests |
| Auto-generator ignores fixed conveyor layout | Treat conveyor/void as fixed constraints before candidate fill |
| Wrapping animation moves across the whole row visually | Use instant wrap setup plus short slide-in animation, or adapt `PlayCellSwap` only after checking visual quality |
| `CellSwap` and item flows have inconsistent post-action gravity | Decide and test every action type explicitly |

## Acceptance Criteria

- Designers can place and save left/right conveyor row segments in the stage editor.
- Invalid conveyor data blocks save/export/generate/playtest with clear errors.
- Generated boards respect pre-placed `Void` and conveyor metadata.
- Unity and stage editor simulations produce the same board after conveyor movement, gravity, and 180-degree rotation.
- Missing conveyor sprites do not break gameplay or editor rendering.
- Conveyor direction remains readable on a cell-filled board.
