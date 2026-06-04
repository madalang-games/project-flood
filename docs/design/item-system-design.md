# Item System Design — project-flood

Date: 2026-06-04  
Status: draft (decisions confirmed)  
Author: (AI – Senior System/Content/UI/UX Design role)  
Relates to: game-design.md §8, ingame-core-design.md §Tap Flow

---

## 1. Overview

Items are player-activated boosters that modify board state outside of the normal tap-group flow. MVP provides three items: **Bomb**, **H-Rocket**, **V-Rocket**. Dev mode provides unlimited inventory via Inspector toggle; Phase 2 introduces earn/purchase mechanics.

Items do not consume a turn. Game ends when turns reach 0; item use is not possible at turns = 0.

---

## 2. Item Definitions

| Item | Effect | Affected Area |
|------|--------|---------------|
| Bomb | Removes all cells in 3×3 centered on target, including center | 9 cells (3×3) |
| H-Rocket | Sweeps target row left → right; stops after first Obstacle destroyed | Entire row until Obstacle or end |
| V-Rocket | Sweeps target column top → bottom; stops after first Obstacle destroyed | Entire column until Obstacle or end |

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

### 4.2 H-Rocket / V-Rocket (linear sweep)

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

**Obstacle = destroy-and-stop.** The Obstacle is removed, then the rocket halts. Cells beyond the Obstacle in that row/column are untouched. This makes Obstacles a meaningful strategic blocker against rockets — level designers can use Obstacle placement to limit rocket reach.

---

## 5. Effect Resolution (Integration with Existing Rules)

Turn check → if `remaining_turns == 0`, item use is blocked (game already ended).

Order of operations after item fires:

```
1. IItemEffect.GetAffectedCells(board, row, col)
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
**Rendered:** All 3 item types always shown (empty state when count = 0).

```
┌────────────────────────────────────────┐
│              BOARD AREA                │
│                                        │
├────────────────────────────────────────┤
│   [Bomb]     [H-Rocket]   [V-Rocket]   │  ← ItemTrayView
│    (3)          (∞)          (1)       │
└────────────────────────────────────────┘
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
  - Item VFX plays (explosion for Bomb; directional streak for Rocket)
  - Effect resolves (§5 flow)
  - Count decrements / ∞ stays
  - Return to Idle

**ItemSelected → Cancelled:**
- Tap same slot again
- Tap outside board area (any non-board UI)
- No item consumed

**Locks during:** VFX playback, board rotation animation, clear/fail overlay.

### 7.4 Range Indicator

No per-cell hover on mobile. Range is indicated on the **slot button** itself:
- Bomb slot: 3×3 dot-grid icon
- H-Rocket slot: horizontal arrow icon
- V-Rocket slot: vertical arrow icon

No dynamic board preview on tap (single-tap executes immediately).

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
public enum ItemType { Bomb, HRocket, VRocket }

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

public class BombEffect : IItemEffect
{
    // 3×3 centered on target, all 9 positions
    // Void positions are filtered by checking board bounds + CellType
}

public class HRocketEffect : IItemEffect
{
    // Sweep targetRow left→right; stop list building after first Obstacle
}

public class VRocketEffect : IItemEffect
{
    // Sweep targetCol top→bottom; stop list building after first Obstacle
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
- After `UseItem()`: drives `GravitySystem` → `ClearEvaluator` (same pipeline as normal tap)

No changes to `GroupSelector`, `RemovalSystem`, `ProtectorSystem`, `GravitySystem`, `ClearEvaluator`, `BoardView`, `CellView`.

---

## 11. Clearance Ratio Impact

Obstacle destroyed by item does **not** change `initial_valid_cells`. The ratio denominator is fixed at stage load (Obstacles are already excluded from it). Destroying an Obstacle has no direct ratio effect — it only opens paths, removes blockers, or (for rockets) limits how far the rocket reaches.

---

## 12. Phased Scope

### MVP
- 3 items: Bomb (3×3), H-Rocket (stops at Obstacle), V-Rocket (stops at Obstacle)
- Dev mode: Inspector toggle, ∞ badge, standard in-game tray UI
- Item Use Phase: slot glow, board target highlight, single-tap execute, cancel
- Full rule engine integration (DirectHit protector, gravity, clear eval)
- No turn cost; no auto-chain

### Phase 2
- Earn mechanics: streak rewards, daily bonus
- IAP purchase flow
- Server-backed inventory persistence
- New item types (candidate: Color Eraser, Shuffle, Shield)
- Shop UI

---

## 13. Risks

| Risk | Severity | Mitigation |
|------|----------|------------|
| Rocket + Obstacle stop creates unintuitive edge cases (Void gap before Obstacle) | Medium | Clear VFX: rocket visually travels and stops; Obstacle destruction animation |
| Bomb trivializes dense Core+Protector clusters | Medium | Stage design: limit accessible Core clusters; Core usually deeper in board |
| Dev mode accidentally shipped to production | Low | `IsDevMode` is Inspector-only; add `#if UNITY_EDITOR` guard or build check |
| Obstacle removal changes board shape expectations | Low | Obstacle is visually distinct; its destruction is a high-value player moment |
