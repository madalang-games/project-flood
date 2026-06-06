# Item System Design — project-flood

Date: 2026-06-04  
Status: draft (decisions confirmed)  
Author: (AI – Senior System/Content/UI/UX Design role)  
Relates to: game-design.md §8, ingame-core-design.md §Tap Flow

---

## 1. Overview

Items are player-activated boosters that modify board state outside of the normal tap-group flow. MVP provides five items: **Bomb**, **H-Rocket**, **ColorSweep**, **RowShift**, **CellSwap**. Dev mode provides unlimited inventory via Inspector toggle; Phase 2 introduces earn/purchase mechanics.

Items do not consume a turn. Game ends when turns reach 0; item use is not possible at turns = 0.

---

## 2. Item Definitions

| Item | Effect | Affected Area |
|------|--------|---------------|
| Bomb | Removes all cells in 3×3 centered on target, including center | 9 cells (3×3) |
| H-Rocket | Sweeps target row left → right; stops after first Obstacle destroyed | Entire row until Obstacle or end |
| ColorSweep | Removes all cells on the board matching the color of the tapped cell | All cells of matching color |
| RowShift | Packs all cells in each row toward the swipe direction; swipe gesture with minimum distance threshold | Entire board (per-row compaction) |
| CellSwap | Swaps the positions of two tapped cells | 2 cells |

---

## 3. Target Validity

Valid tap = any non-null, non-Void cell.

| Tap target | Valid? | Reason |
|------------|--------|--------|
| Basic / Protector / Core | Yes | Primary target |
| Obstacle | Yes | Valid position; destroyed by item |
| Void | **No** | Non-existent position; nothing to reference |
| Empty slot (null) | **No** | Nothing to target |

**Invalid tap behavior:** Silently ignored. Player remains in Use Phase and can re-tap.

*RowShift exception: uses swipe gesture, not tap. Target validity does not apply; swipe is captured at board level.*

---

## 4. Void & Obstacle Policy

### 4.1 Bomb (3×3, fixed radius — no travel)

No travel direction. All 9 cells in the 3×3 grid resolve simultaneously.

| Cell in 3×3 radius | Result |
|--------------------|--------|
| Basic / Core (no protector) | Removed |
| Protector cell | One layer stripped (DirectHit) |
| Obstacle | **Removed** |
| Void position | Skipped (non-existent) |
| Empty (null) | Skipped |

### 4.2 H-Rocket (linear sweep)

Rocket travels cell-by-cell in sweep direction.

| Cell encountered | Result | Continues? |
|------------------|--------|------------|
| Basic / Core (no protector) | Removed | Yes |
| Protector cell | One layer stripped (DirectHit) | Yes |
| Void position | Skipped (non-existent, position has no cell) | **Yes** |
| Obstacle | **Removed** | **No — stops here** |
| Empty (null) | Skipped | Yes |
| Board edge | — | Terminates |

**Void = skip-continue.** Void is a board shape boundary (invisible, non-existent cell). The rocket skips the position and continues to the next cell. Non-rectangular boards would otherwise make rockets unpredictably short.

**Obstacle = destroy-and-stop.** The Obstacle is removed, then the rocket halts. Cells beyond the Obstacle in that row are untouched. This makes Obstacles a meaningful strategic blocker against rockets — level designers can use Obstacle placement to limit rocket reach.

### 4.3 ColorSweep (board-wide color removal)

All cells on the board sharing the color ID of the tapped cell are targeted simultaneously.

| Cell type | Result |
|-----------|--------|
| Basic / Core (matching color, no protector) | Removed |
| Protector cell (matching color) | One layer stripped (DirectHit) |
| Obstacle | Not affected (Obstacles have no color ID) |
| Void position | Skipped |
| Empty (null) | Skipped |
| Any cell with different color ID | Not affected |

**Protector rule:** ColorSweep applies DirectHit to each matching cell individually. A cell with protector strength > 0 loses one layer but is not removed. Player may ColorSweep again after gravity to remove the now-unprotected cell.

### 4.4 RowShift (horizontal compaction)

RowShift is triggered by a horizontal swipe gesture on the board. In each row, valid (non-Void) cells slide to eliminate empty (null) slots, compacting toward the swipe direction. Void positions act as hard boundaries; each contiguous valid segment of a row shifts independently within its valid range.

| Cell | Behavior |
|------|----------|
| Basic / Protector / Core / Obstacle | Slides with the compaction |
| Void position | Immovable boundary; segments on each side shift independently |
| Empty slot (null) | Filled in by sliding cells; becomes empty on the trailing edge |

After compaction, `GravitySystem.Apply()` runs to re-settle any cells displaced vertically.

**Swipe gesture:** Minimum swipe distance threshold required to register direction (left or right). Short or ambiguous swipes are ignored; player remains in Use Phase.

### 4.5 CellSwap (two-cell position swap)

Two valid cells are tapped sequentially. Their positions are exchanged on the board, then `GravitySystem.Apply()` runs.

| Cell pair | Valid? | Result |
|-----------|--------|--------|
| Any two valid cells (non-null, non-Void) | Yes | Positions swapped |
| First or second cell is Void or null | No | Invalid tap; stay in selection state |

---

## 5. Effect Resolution (Integration with Existing Rules)

Turn check → if `remaining_turns == 0`, item use is blocked (game already ended).

Order of operations after item fires:

```
1. IItemEffect.GetAffectedCells(board, row, col)         ← Bomb, H-Rocket, ColorSweep, CellSwap
   IRowShiftEffect.Apply(board, direction)                ← RowShift (no target cell)
   → respects Void/Obstacle rules per §4
2. For each cell in affected list:
     ProtectorSystem.DirectHit(cell)
       → strength > 0: strip one layer (cell stays, update sprite)
       → strength == 0: board[r,c] = null (removed)
3. GravitySystem.Apply(board)
4. ItemInventory.Consume(type)           ← skipped in Dev mode
5. ClearEvaluator.Evaluate(board, ...)
6. InGameController handles StarResult
```

Items do NOT auto-chain. Player may use multiple items manually in sequence before spending a turn.

---

## 6. Turn & Game End Policy

- Item use does **not** consume a turn.
- When `remaining_turns == 0`: stage ends immediately with current board state → `ClearEvaluator` runs → result shown. No item use possible after turns reach 0.
- Turn 0 is reached only via normal tap-group (`TurnManager.Consume()`). Item use cannot drive turns to 0.

---

## 7. UI Design

### 7.1 Item Tray Layout

**Position:** Fixed at screen bottom, above safe area inset.  
**Component:** `HorizontalLayoutGroup`, centered alignment, equal spacing.  
**Rendered:** All 5 item types always shown (empty state when count = 0).

```
┌─────────────────────────────────────────────────────────────┐
│                        BOARD AREA                           │
│                                                             │
├─────────────────────────────────────────────────────────────┤
│  [Bomb] [H-Rocket] [ColorSweep] [RowShift] [CellSwap]       │  ← ItemTrayView
│   (3)     (∞)         (1)          (2)        (5)           │
└─────────────────────────────────────────────────────────────┘
```

### 7.2 Item Slot States

| State | Visual | Condition |
|-------|--------|-----------|
| Normal | Icon + count badge | count > 0 or Dev mode |
| Empty | Greyed icon, no badge | count = 0, not Dev mode |
| Selected | Highlight ring / glow | This item's Use Phase active |
| Locked | Dimmed, non-interactive | During VFX, board rotation, or clear/fail overlay |

**Dev mode badge:** Shows "∞". No other difference.

### 7.3 Item Use Phase — Interaction Flow

**Bomb / H-Rocket / ColorSweep (tap-to-target):**

States: `Idle → ItemSelected → (TargetTapped | Cancelled) → Idle`

**Idle → ItemSelected:**
- Player taps slot with count > 0 (or Dev mode)
- Slot enters Selected state (glow animation)
- Board enters targeting mode: valid cells (non-null, non-Void) get subtle pulse/outline
- HUD: item name + one-line description appears
- Tap same slot again → cancel

**ItemSelected → TargetTapped:**
- Player taps board cell
- Hit test → (row, col)
- Invalid (Void or null): no-op, stay in Use Phase
- Valid: execute item immediately (single-tap, no confirm step)
  - Item VFX plays (explosion for Bomb; directional streak for Rocket; color-wave for ColorSweep)
  - Effect resolves (§5 flow)
  - Count decrements / ∞ stays
  - Return to Idle

**ItemSelected → Cancelled:**
- Tap same slot again
- Tap outside board area (any non-board UI)
- No item consumed

---

**RowShift (swipe-to-shift):**

States: `Idle → ItemSelected → (SwipeDetected | Cancelled) → Idle`

**Idle → ItemSelected:** Same as tap-to-target flow above.

**ItemSelected → SwipeDetected:**
- Player performs horizontal swipe on the board
- Swipe distance ≥ threshold: detect direction (left / right) → execute RowShift
  - VFX: all cells animate sliding in swipe direction
  - Effect resolves (§5 flow — RowShift path)
  - Count decrements / ∞ stays
  - Return to Idle
- Swipe distance < threshold or vertical swipe: no-op, stay in Use Phase
- Tap on board (no swipe): no-op, stay in Use Phase

**ItemSelected → Cancelled:** Same as tap-to-target flow above.

---

**CellSwap (two-tap):**

States: `Idle → ItemSelected → FirstCellSelected → (SecondCellTapped | Cancelled) → Idle`

**Idle → ItemSelected:** Same as tap-to-target flow above.

**ItemSelected → FirstCellSelected:**
- Player taps a valid board cell
- Cell enters Selected highlight
- HUD updates: "tap second cell"

**FirstCellSelected → SecondCellTapped:**
- Player taps a second valid board cell (different from first)
- Swap executes immediately
  - VFX: two cells animate to each other's positions
  - Effect resolves (§5 flow)
  - Count decrements / ∞ stays
  - Return to Idle
- Tap on first cell again → deselect first cell, return to ItemSelected state
- Tap on invalid cell (Void / null): no-op, stay in FirstCellSelected

**FirstCellSelected → Cancelled:**
- Tap same slot again
- Tap outside board area
- No item consumed

**Locks during:** VFX playback, board rotation animation, clear/fail overlay.

### 7.4 Range Indicator

No per-cell hover on mobile. Range is indicated on the **slot button** itself:
- Bomb slot: 3×3 dot-grid icon
- H-Rocket slot: horizontal arrow icon
- ColorSweep slot: color-wave / palette icon
- RowShift slot: double horizontal arrow icon (←→)
- CellSwap slot: swap/exchange arrows icon

No dynamic board preview on tap (single-tap executes immediately for tap-to-target items).

---

## 8. Dev Mode

`IsDevMode` is a Unity Inspector bool field on `ItemInventory` (or its owning MonoBehaviour).

| Behavior | Normal | Dev mode |
|----------|--------|----------|
| Count decrements on use | Yes | No |
| Badge text | Number | ∞ |
| Items available when count = 0 | No | Yes (always available) |
| UI / UX | Same | Same |
| Separate dev panel | — | None needed |

Flip `IsDevMode = true` in Inspector → all items immediately usable from the standard in-game tray. No additional UI or tooling required.

---

## 9. Data Model

### ItemInventory (pure C#, no UnityEngine)

```csharp
// Game.InGame.Items
public enum ItemType { Bomb, HRocket, ColorSweep, RowShift, CellSwap }

public class ItemInventory
{
    public bool IsDevMode;
    private readonly Dictionary<ItemType, int> _counts;

    public bool CanUse(ItemType type) => IsDevMode || _counts.GetValueOrDefault(type) > 0;
    public void Consume(ItemType type) { if (!IsDevMode) _counts[type]--; }
    public int GetCount(ItemType type) => _counts.GetValueOrDefault(type);
}
```

### Item Effect Interface (pure C#)

```csharp
// Game.InGame.Items
public interface IItemEffect
{
    // Returns ordered list of (row, col) to attempt removal.
    // Callee must still check board state per §4 (Void skip, Obstacle stop-after).
    List<(int row, int col)> GetAffectedCells(BoardState board, int targetRow, int targetCol);
}

// RowShift uses a separate interface (no target cell; direction-based)
public interface IRowShiftEffect
{
    void Apply(BoardState board, ShiftDirection direction);
}

public enum ShiftDirection { Left, Right }

public class BombEffect : IItemEffect
{
    // 3×3 centered on target, all 9 positions
    // Void positions filtered by checking board bounds + CellType
}

public class HRocketEffect : IItemEffect
{
    // Sweep targetRow left→right; stop list building after first Obstacle
}

public class ColorSweepEffect : IItemEffect
{
    // Collect all cells on board where cell.ColorId == board[targetRow, targetCol].ColorId
    // Obstacles excluded (no color ID)
}

public class RowShiftEffect : IRowShiftEffect
{
    // For each row: collect valid (non-Void) cells in order
    // Pack them toward Left or Right edge of the valid segment
    // Fill trailing empty slots with null
}

public class CellSwapEffect : IItemEffect
{
    // Returns both cell positions; caller performs the swap
    // GetAffectedCells used for validation; actual swap logic in ItemManager
}
```

---

## 10. Architecture Integration

New namespace: `Game.InGame.Items`

```
InGameController
  ├── (existing) GroupSelector, RemovalSystem, GravitySystem, TurnManager, ClearEvaluator
  ├── (new) ItemManager       — pure C#; owns ItemInventory + IItemEffect instances; tracks UsePhase state
  └── (new) ItemTrayView      — MonoBehaviour; renders slots; fires slot-tap events to InGameController
```

**InGameController additions:**
- Owns `ItemManager`
- On slot tap from `ItemTrayView` → `ItemManager.SelectItem(type)` (if `remaining_turns > 0`)
- On board cell tap → if `ItemManager.IsInUsePhase`: dispatch to `ItemManager.UseItem(row, col)` instead of normal GroupSelector flow
- On board swipe → if `ItemManager.IsInUsePhase && selectedItem == RowShift`: dispatch to `ItemManager.UseRowShift(direction)`
- After `UseItem()` / `UseRowShift()`: drives `GravitySystem` → `ClearEvaluator` (same pipeline as normal tap)

No changes to `GroupSelector`, `RemovalSystem`, `ProtectorSystem`, `GravitySystem`, `ClearEvaluator`, `BoardView`, `CellView`.

---

## 11. Clearance Ratio Impact

Obstacle destroyed by item does **not** change `initial_valid_cells`. The ratio denominator is fixed at stage load (Obstacles are already excluded from it). Destroying an Obstacle has no direct ratio effect — it only opens paths, removes blockers, or (for rockets) limits how far the rocket reaches.

---

## 12. Phased Scope

### MVP
- 5 items: Bomb (3×3), H-Rocket (stops at Obstacle), ColorSweep (all same-color), RowShift (horizontal compaction, swipe gesture), CellSwap (two-cell swap)
- Dev mode: Inspector toggle, ∞ badge, standard in-game tray UI
- Item Use Phase: slot glow, board target highlight, single-tap execute (tap-to-target), swipe execute (RowShift), two-tap execute (CellSwap), cancel
- Full rule engine integration (DirectHit protector, gravity, clear eval)
- No turn cost; no auto-chain

### Phase 2
- Earn mechanics: streak rewards, daily bonus
- IAP purchase flow
- Server-backed inventory persistence
- New item types (candidate: ColorChange, Shield)
- Shop UI

---

## 13. Risks

| Risk | Severity | Mitigation |
|------|----------|------------|
| Rocket + Obstacle stop creates unintuitive edge cases (Void gap before Obstacle) | Medium | Clear VFX: rocket visually travels and stops; Obstacle destruction animation |
| Bomb trivializes dense Core+Protector clusters | Medium | Stage design: limit accessible Core clusters; Core usually deeper in board |
| RowShift swipe threshold: too low → accidental activations, too high → unresponsive | Medium | Tune swipe threshold in playtest; expose as configurable constant |
| CellSwap misuse on Protector cells (strips layer, wastes item) | Low | One-line HUD hint when CellSwap selected: shows swap, not removal |
| ColorSweep on dominant color clears too many cells (trivializes stage) | Low | Stage design: avoid single dominant-color boards in early stages |
| Dev mode accidentally shipped to production | Low | `IsDevMode` is Inspector-only; add `#if UNITY_EDITOR` guard or build check |
