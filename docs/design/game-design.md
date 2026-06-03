# Game Design Document — project-flood

## 1. Concept

Mobile puzzle game. Player taps same-color cell groups to remove them from a board within a turn limit. Core loop: remove cells → gravity collapses board → clear ratio determines stars → retry for higher stars.

Differentiators vs SameGame/Collapse:
- Ratio-based star system with hard core-cell gate
- Pixel art board aesthetic
- Gimmick cells (Bomb, Rocket, Protector, Core)
- Image-based stages (Phase 2)
- Board rotation (Phase 2)

---

## 2. Board

| Stage Type | Board Size |
|------------|------------|
| Normal (early) | 8×8 – 12×12 |
| Normal (mid-late) | 12×12 – 14×14 |
| Challenge / Image / Boss | up to 16×16 |

- Size is defined per stage in editor.
- 16×16 is not the default for standard main stages.

---

## 3. Color System

- **Master palette**: 16 pre-defined colors (`color_palette` table).
- **Per-stage colors**: up to 8 color IDs selected from palette.
- **Image-to-board mapping**: LAB color space nearest-color mapping (perceptually accurate).
- Palette is expandable (add rows) without schema change.

### color_palette table
```
color_id (0–15) | r | g | b | name
```

---

## 4. Core Gameplay Rules

### 4.1 Cell Selection
- Tap a cell → BFS finds all 4-directionally adjacent cells of same color.
- Diagonal adjacency is NOT same group.
- All taps are valid; turn consumed regardless of group size.
- BFS returns the same-color connected group from the tapped cell (minimum size 1).
- Isolated cells (size=1) are removed normally — no permanent stuck state. (ADR-004)
- Turn consumed only on valid removal.

### 4.2 Gravity
- After removal: floating cells fall downward.
- No horizontal compression (empty columns stay empty).
- Applied immediately after each removal resolves.

---

## 5. Cell Types

### 5.1 Basic Cell
Normal colored cell. Removed when part of a valid group tap.

### 5.2 Gimmick Cells

Gimmick cells are introduced progressively as the player advances through stages.

#### Protector Cell
- Only on Basic cells; inherits the same color as the underlying Basic cell.
- Strength: 1 or 2 layers (editor-defined per cell).
- Reaction: **direct hit only** — a protector layer is stripped by same-color group tap (this cell is in the matched group) or item applied directly to this cell.
- Adjacent cell removal does NOT strip protector.
- After all protector layers removed: underlying Basic cell is exposed.
- Protector cell participates in same-color BFS (can be part of a valid group).

#### Core Cell
- A hard win-condition gate; introduced in later stages.
- If any core cell is NOT removed when the stage ends → **FAIL**, regardless of ratio.
- Core cells count toward the clearance ratio denominator and numerator.
- May have Protector stacked on top (editor-defined).

#### Obstacle Cell (Non-interactive)
- Cannot be selected or removed by any means (group tap, gimmick, item).
- **Excluded from `initial_valid_cells`** in clearance ratio.
- Visually distinct from normal cells.

### 5.3 Protector Strip Rules

| Event | Strips Protector Layer? |
|-------|------------------------|
| Cell's own same-color group tapped (cell is in the matched group) | Yes |
| Item applied directly to this cell | Yes |
| Adjacent cell removed (any cause) | **No** |

---

## 6. Clear Conditions

### 6.1 Stage End Triggers
1. **Turn exhausted**: auto-end → ratio calculated on final board state.
2. **All valid cells cleared**: early termination → 3-star awarded immediately.

### 6.2 Win / Fail Logic
```
initial_valid_cells = total_board_cells - obstacle_cells
remaining_cells     = valid cells still on board at stage end
clearance_ratio     = (initial_valid_cells - remaining_cells) / initial_valid_cells

WIN  = clearance_ratio >= star1_ratio
       AND all core cells removed (if stage has core cells)

FAIL = clearance_ratio < star1_ratio
       OR any core cell not removed
```

### 6.3 Star System

| Stars | Condition |
|-------|-----------|
| 3 | All valid cells cleared (early termination) |
| 2 | clearance_ratio >= star2_ratio |
| 1 | clearance_ratio >= star1_ratio |
| Fail | Below star1_ratio OR core cell not removed |

Default thresholds (overridable per stage):
```
star1_ratio = 0.80
star2_ratio = 0.90
star3_ratio = 1.00
```

### 6.4 star_threshold_config table
```
config_id | stage_id (NULL = global default) | star1_ratio | star2_ratio | star3_ratio
```
Referenced by both client and server.

---

## 7. Stage Progression

- Stages unlock sequentially: WIN on stage N → stage N+1 unlocked.
- WIN = any star result (≥ 1 star).
- All previously cleared stages are replayable at any time.
- Per-stage persistence: `best_star`, `best_move_count`.

---

## 8. Items (Boosters)

- One-time use; persist in inventory until consumed.
- Player manually applies to a specific cell during gameplay.
- Item effect can trigger board gimmick chain reactions (follows §5.3 rules).
- Items do NOT auto-chain with other inventory items.
- MVP item effects: Bomb (removes all 8-directional adjacent cells), Horizontal Rocket (clears row), Vertical Rocket (clears column).
- Item effects trigger board state changes identically to §4 rules (gravity applies after).
- **MVP**: Dev-only, controlled via Unity Inspector. No in-game UI.
- Streak reward system and IAP integration: **Phase 2**.

---

## 9. Stage Data Pipeline

```
Editor → shared/datas/stage/*.csv → info_generator → client/generated/data/
```

- All stage data includes `rulesetVersion` to lock replay fidelity of `verifiedSolution`.
- Stage data format: `stageId`, `boardWidth`, `boardHeight`, `turnLimit`, `difficulty`, `colorIds`, `star1Ratio`, `star2Ratio`, `cells`, `verifiedSolution`, `rulesetVersion`.
- Cell encoding: CTM hex (3 hex chars per cell, flat row-major string). `C`=color_id (0–F), `T`=CellType (0=Basic,1=Obstacle), `M`=modifier bitmask (bits[1:0]=protector_strength, bit[2]=is_core). See ADR-003.

---

## 10. Image Stage System (Phase 2 Draft)

Pipeline:
1. Import source image.
2. Pixelate to target board size.
3. Map to 16-color palette (LAB nearest-color).
4. Correct isolated single cells.
5. Designate core cells manually in editor.
6. Editor play-test + manual review.
7. Export as stage CSV.

Design principle: **auto-draft + manual edit + validate** (not full auto-generation).

Retention hooks: album collection, 3-star frame rewards, daily image puzzles (Phase 2).

---

## 11. Stage Editor

Core development risk. Editor is required before content production begins.

### MVP Required Features
| Feature | Notes |
|---------|-------|
| Board size config | |
| Cell color placement | palette color picker |
| Core cell designation | |
| Bomb / H-Rocket / V-Rocket placement | |
| Protector application | stackable on any cell |
| Obstacle cell placement | |
| Turn limit setting | |
| Star threshold config | per-stage override |
| Play-test mode | in-editor simulation |
| verifiedSolution recording | records editor's clear sequence |
| Stage data export | outputs to `shared/datas/stage/` |

### Phase 2 Editor Features
- Board rotation rule config
- Color hide rule config
- Image-to-board conversion
- Auto validation (Solver integration)
- Difficulty tag auto-suggestion
- Seed-based stage generation

---

## 12. Solver & Validation

MVP approach: **verifiedSolution replay**, not full solver.

1. Editor operator clears the stage manually.
2. Clear sequence saved as `verifiedSolution`.
3. Rule engine replays the sequence and confirms stage is clearable.

This guarantees clearability without requiring exhaustive search.

Heuristic checks (MVP):
- Obvious impossible state detection (e.g., core cell surrounded only by obstacle cells).
- Isolated single-cell count detection.
- Rough difficulty estimation.

Full solver: Phase 2+.

---

## 13. MVP Scope

### Included
- Dynamic board size (editor-defined)
- Color group selection + removal
- Downward gravity, no horizontal compression
- Turn limit
- Ratio-based star system (80 / 90 / 100%)
- Core cell gimmick (late stages)
- Protector cells (Basic cells only, 1–2 layers, direct-hit strip rule)
- Bomb, Horizontal Rocket, Vertical Rocket as dev-only items (Inspector)
- Obstacle cells (data + ratio exclusion)
- Stage select UI + replay cleared stages
- Best star / best move count saved per stage
- Per-stage `star1_ratio` / `star2_ratio` inline in stage data (star3 = full clear, no ratio needed)
- `color_palette` table (16 colors, IDs 0–15)
- Dev-only item system via Inspector
- 30 handcrafted stages
- verifiedSolution replay validation

### Excluded (Phase 2+)
- Full solver
- Server-based stage delivery
- User image upload
- Board rotation gimmick
- Color hide gimmick
- Album / collection system
- Daily challenge
- Streak reward UI / system
- Season system
- In-game item UI / shop
- Infinite auto-generation

---

## 14. Risks

| Risk | Severity | Mitigation |
|------|----------|------------|
| SameGame similarity | Medium | Core cell gate, pixel art, image stages, rotation (P2) |
| Content production bottleneck | High | Editor is P0; verifiedSolution validates each stage |
| Solver complexity growth | Medium | Heuristic only in MVP; rulesetVersion locks replay fidelity |
| Last-cell frustration | Low | Ratio-based: no single isolated cell blocks base clear |
| Retention drop-off | Medium | Star retry loop; image stages + album (P2) |
| Item balance (3-star access) | Low | Items = player strategy; 3-star via item is valid |

---

## 15. Validation Goals (MVP)

Core questions to answer before Phase 2 investment:

- Do players understand the rules immediately?
- Is one session length appropriate (not too long / short)?
- Does failure generate "retry" intent?
- Does the 3-star condition create replayability?
- Do Bomb / Rocket effects feel satisfying?
- Is the core cell gate clear and fair?
- Does the pixel art style provide emotional reward?

Threshold: players voluntarily play for 10+ minutes on first session.
