# FTUE & Tutorial System Design — project-flood

Date: 2026-06-04
Status: draft
Relates to: game-design.md, ingame-core-design.md, ui-ux-common-components.md §13, ui-ux-canvas-architecture.md, economy-system-design.md

---

## 1. Goals & Principles

**Goals**
- Prevent D1 churn: guarantee first-session rule comprehension and first clear
- Progressive disclosure: Core Loop → Gimmick → Item, one concept per step
- Minimize friction: forced guidance limited to Stage 1 only; all later steps are contextual

**Design Rules**
1. One line of text maximum — teach through visual cues, not descriptions
2. One concept per step — never explain Protector and Core simultaneously
3. Reward before teaching — Stage 1 guarantees a win before player understands why
4. No repeat — each tutorial group is recorded on server after its final step; never replayed
5. No emojis in client-facing strings — icons are separate UI assets, not inline text

---

## 2. Tutorial Phases

```
Phase A (Forced)       Stage 1~3    Core Loop acquisition
Phase B (Contextual)   Stage 4+     Gimmick explanation on first encounter
Phase C (Fail-based)   Any stage    Item hint after 3 consecutive failures
```

---

## 3. Architecture

### 3.1 Canvas Layer

`TutorialOverlay` lives on `Canvas_Overlay (Sort: 20)` per `ui-ux-canvas-architecture.md`.

```
[UIManager — DontDestroyOnLoad]
  └── Canvas_Overlay (Sort: 20)
        └── TutorialOverlay          ← instantiated/destroyed via UIManager.ShowOverlay<TutorialOverlay>
              ├── DimLayer           full-screen dim, RaycastBlock (alpha 0.7)
              ├── SpotlightCutout    punches a hole in DimLayer revealing target
              ├── FingerOverlay      animated tap hand above target
              └── TooltipBubble      speech bubble anchored to target
```

Toast (Sort: 30) does not appear while TutorialOverlay is active — no z-order conflict.

### 3.2 New Components

| Component | Type | Role |
|-----------|------|------|
| `TutorialManager` | MonoBehaviour (DDOL) | Owns step sequencer; evaluates triggers on scene events |
| `TutorialStepSequencer` | Pure C# | Advances steps; reads tutorial_step data; fires display commands |
| `TutorialOverlay` | MonoBehaviour (Overlay prefab) | Renders DimLayer + SpotlightCutout + FingerOverlay + TooltipBubble |
| `SpotlightTarget` | Pure C# (data) | Carries targeting mode (UI / World) and target reference |

### 3.3 InGameController Integration

Minimal surface — no existing classes modified beyond two call sites:

```csharp
// InGameController.HandleTap()
if (TutorialManager.Instance.IsBlocking) return;   // Phase A forced tap guard

// InGameController.OnBoardReady()
TutorialManager.Instance.OnBoardReady(stageId, board);   // triggers Phase A/B evaluation
```

### 3.4 Lobby Integration

```csharp
// LobbyController.OnSceneEnter()
TutorialManager.Instance.CheckLobbyTriggers();
```

---

## 4. SpotlightOverlay — Targeting System

Two targeting modes. The mode is determined per step by the `target_space` field.

### 4.1 UI Mode (`target_space = UI`)

Used when `target_ui_id` refers to a UI element with a `RectTransform` (HUD, item tray, result screen).

```csharp
// Convert RectTransform to Canvas_Overlay local position
RectTransformUtility.ScreenPointToLocalPointInRectangle(
    overlayCanvas.GetComponent<RectTransform>(),
    RectTransformUtility.WorldToScreenPoint(null, targetRt.position),
    overlayCanvas.worldCamera,
    out Vector2 localPoint
);
spotlightCutout.anchoredPosition = localPoint;
spotlightCutout.sizeDelta = targetRt.rect.size * targetRt.lossyScale / overlayCanvas.scaleFactor;
```

Re-evaluate on `OnRectTransformDimensionsChange` (handles orientation change, safe area shift).

### 4.2 World Mode (`target_space = World`)

Used when `target_ui_id` refers to a board cell or other scene-space GameObject (e.g., `board_cell_[r][c]`).

```csharp
// Convert world position to Canvas_Overlay local position
Vector3 screenPos = Camera.main.WorldToScreenPoint(targetWorldPos);
RectTransformUtility.ScreenPointToLocalPointInRectangle(
    overlayCanvas.GetComponent<RectTransform>(),
    screenPos,
    overlayCanvas.worldCamera,
    out Vector2 localPoint
);
spotlightCutout.anchoredPosition = localPoint;
// Size derived from world-space bounds projected to screen at runtime
```

`CellView` exposes `GetWorldCenter()` and `GetScreenBounds()` — `SpotlightCutout` reads these.
Re-evaluate on every frame during board animation (board scales/repositions on load).

### 4.3 Responsive Recalculation

| Event | Action |
|-------|--------|
| `OnRectTransformDimensionsChange` | Recalculate spotlight position + size |
| Board animation frame | Recalculate (World mode only, during load sequence) |
| `Screen.safeArea` change | `TutorialManager` re-triggers `OnBoardReady` positioning |

---

## 5. Phase A — Core Loop Onboarding (Stages 1–3)

### 5.1 Stage 1 — Forced Guidance

**Board Requirements (Stage 1 dedicated):**

| Field | Value | Rationale |
|-------|-------|-----------|
| board_width | 6 | Small — reduces cognitive load |
| board_height | 6 | Small |
| turn_limit | 20 | Generous — player cannot run out |
| color_count | 3 | Minimum color variety |
| star1_ratio | 0.70 | Below default 0.80 — guarantees pass even with suboptimal play |
| star2_ratio | 0.85 | |
| difficulty | tutorial | |
| gimmicks | none | No Core / Protector / Obstacle |

**3-Touch Clear Guarantee (board layout contract):**

Stage 1 must be designed so that 3 taps on the 3 largest groups clears >= 70% of the board.
The Stage Editor operator must verify this with `verifiedSolution` before export.
Suggested layout: three large monochrome clusters (8+ cells each), no isolated singles.

**Step Sequence:**

| step_index | content_type | target_ui_id | target_space | text_key | auto_advance_sec | is_blocking |
|-----------|-------------|-------------|-------------|---------|-----------------|------------|
| 0 | `finger_overlay` | `board_cell_[r][c]` (largest group representative cell) | World | `tut.tap_group` | 0 | true |
| 1 | `tooltip` | `board_area` | World | `tut.gravity_explain` | 2.0 | false |
| 2 | `highlight_only` | `hud_turn_count` | UI | `tut.turn_explain` | 2.0 | false |
| 3 | `highlight_only` | `hud_ratio_bar` | UI | `tut.ratio_explain` | 2.0 | false |
| 4 | `tooltip` | `result_star_area` | UI | `tut.star_explain` | 0 | false |

Step 4 advances on any Result button tap.

**Forced tap behavior (step 0):**
- `DimLayer` active (alpha 0.7, full RaycastBlock)
- `SpotlightCutout` reveals the entire connected group of the largest color cluster (all cells in the group, not just the representative)
- `FingerOverlay` positioned over the representative cell (group centroid)
- All other board cells and HUD are RaycastBlocked
- Player tap on any non-spotlight cell → silently ignored

### 5.2 Stage 2 — Semi-guided

| step_index | content_type | target_ui_id | target_space | text_key | auto_advance_sec | is_blocking |
|-----------|-------------|-------------|-------------|---------|-----------------|------------|
| 0 | `highlight_only` | `board_area` | World | `tut.free_tap` | 0 | false |

Step advances on first player tap. No forced direction.

### 5.3 Stage 3 — Free Play

No tutorial steps. Phase A completion flag set after Stage 3 entry.

---

## 6. Phase B — Contextual Gimmick Introduction

Triggers on first encounter of each gimmick type. `trigger_type = gimmick_appear`.
Evaluated in `OnBoardReady()` — checks current BoardState for gimmick types not yet recorded in `user_tutorial_progress`.

### 6.1 Protector Cell

| step_index | content_type | target_ui_id | target_space | text_key | auto_advance_sec | is_blocking |
|-----------|-------------|-------------|-------------|---------|-----------------|------------|
| 0 | `highlight_only` | `board_protector_cell` (first protector cell) | World | `tut.protector_what` | 2.0 | false |
| 1 | `finger_overlay` | `board_protector_cell` | World | `tut.protector_how` | 2.0 | false |

Step 1 auto-advances at 2s or when the protector cell is tapped (whichever is first).

### 6.2 Core Cell

| step_index | content_type | target_ui_id | target_space | text_key | auto_advance_sec | is_blocking |
|-----------|-------------|-------------|-------------|---------|-----------------|------------|
| 0 | `tooltip` | `board_core_cell` (first core cell) | World | `tut.core_warning` | 3.0 | false |

Warning icon rendered as a separate UI asset positioned adjacent to TooltipBubble — not embedded in text.

### 6.3 Obstacle Cell

| step_index | content_type | target_ui_id | target_space | text_key | auto_advance_sec | is_blocking |
|-----------|-------------|-------------|-------------|---------|-----------------|------------|
| 0 | `tooltip` | `board_obstacle_cell` (first obstacle cell) | World | `tut.obstacle_what` | 3.0 | false |

### 6.4 Board Rotation — Phase 2 (spec only)

| step_index | content_type | target_ui_id | target_space | text_key | auto_advance_sec | is_blocking |
|-----------|-------------|-------------|-------------|---------|-----------------|------------|
| 0 | `tooltip` | `board_area` | World | `tut.rotation_explain` | 0 | false |

Shown before rotation animation plays. Auto-advances when rotation animation completes.

---

## 7. Phase C — Fail-triggered Item Hint

### 7.1 Trigger Condition

```
fail_count[stage_id] >= 3
AND ItemInventory.CanUse(any type) == true
AND PlayerPrefs.GetInt("item_hint_shown_" + stage_id) == 0
```

`item_hint_shown` stored in `PlayerPrefs` (per stage_id). Server record not required.

### 7.2 Step Sequence

| step_index | content_type | target_ui_id | target_space | text_key | auto_advance_sec | is_blocking |
|-----------|-------------|-------------|-------------|---------|-----------------|------------|
| 0 | `finger_overlay` | `item_tray` | UI | `tut.item_hint_prompt` | 3.0 | false |
| 1 | `tooltip` | `item_slot_bomb` | UI | `tut.item_bomb_effect` | 2.0 | false |

Both steps are non-blocking. Auto-advances on timer if player does not interact.
Set `item_hint_shown` in PlayerPrefs after step 1 regardless of player action.

### 7.3 No Items Available (Phase 2)

When `ItemInventory.CanUse(any) == false`: hint is suppressed in MVP.
Phase 2: show rewarded ad CTA instead ("Watch ad to get an item").

---

## 8. Data Schema

Extends `tutorial_step` table from `ui-ux-common-components.md §13`. Two columns added.

### tutorial_step (CSV → info_generator)

| Column | Type | Notes |
|--------|------|-------|
| id | INT PK | |
| trigger_type | ENUM | `first_launch` / `gimmick_appear` / `fail_repeat` / `stage_clear` / `chapter_clear` |
| trigger_value | VARCHAR | stage_id, gimmick type name, fail threshold. NULL for `first_launch`. |
| step_index | INT | Order within same trigger group (0-based) |
| content_type | ENUM | `finger_overlay` / `tooltip` / `highlight_only` |
| target_ui_id | VARCHAR | Logical ID resolved at runtime to RectTransform or world GameObject |
| target_space | ENUM | `UI` (RectTransform) / `World` (scene world position) |
| text_key | VARCHAR | Localization key. No emojis — icons attached via separate UI asset |
| auto_advance_sec | FLOAT | >0: auto-advance after N seconds. 0: wait for player action |
| is_blocking | BOOL | true: all input blocked except spotlight target (Phase A forced taps only) |

### user_tutorial_progress (server)

| Column | Type |
|--------|------|
| user_id | INT FK |
| tutorial_id | INT FK → tutorial_step.id (trigger group unit) |
| viewed_at | DATETIME |

**Record timing:** After the final step of a trigger group completes.
**Check timing:** In `OnBoardReady()` and `CheckLobbyTriggers()` before any step plays.
**Guest fallback:** PlayerPrefs key `tut_done_{tutorial_id}`. Migrate to server on account link (Phase 2).

---

## 9. UI Components

### TooltipBubble

```
[Speech bubble panel]
  ├── [Icon slot]    — optional; separate Image asset (no emoji in text)
  └── [TMP_Text]     — text_key binding; TMP Auto Sizing min 12dp, max designed size
[Tail sprite]         — auto-rotates to point at target (4 directions: up/down/left/right)
```

Appear: scale 0.85→1.0, alpha 0→1 (200ms ease-out).
Dismiss: scale 1.0→0.9, alpha 1→0 (150ms ease-in) → advance step.

### FingerOverlay

```
Position: target world/screen center + offset (30dp right, 20dp down)
Animation: scale 1.0 → 0.85 (80ms ease-in) → 1.0 (60ms ease-out), loop period 0.6s
```

### SpotlightCutout

```
DimLayer: full-screen Image (alpha 0.7, color UI_BG_DEEP), RaycastTarget = true
SpotlightCutout: RectMask2D or custom shader cutout
  — position/size set by targeting system (§4)
  — RaycastTarget = false on cutout region only
Resize animation: DOTween SizeDelta 150ms ease-out when step changes target
```

---

## 10. Localization Keys

No emojis in any key value. Icons provided by separate UI Image assets.

| key | EN text | KR text |
|-----|---------|---------|
| `tut.tap_group` | Tap connected cells of the same color! | 같은 색 셀들을 탭하세요! |
| `tut.gravity_explain` | Cells fall downward | 셀이 아래로 떨어져요 |
| `tut.turn_explain` | Use turns wisely to clear more cells | 턴을 아껴서 더 많이 지워보세요 |
| `tut.ratio_explain` | Fill this bar to earn more stars | 이 바를 채울수록 별을 더 받아요 |
| `tut.star_explain` | 3 stars means a perfect clear! | 별 3개면 완벽 클리어! |
| `tut.free_tap` | Now try tapping on your own! | 이제 직접 탭해보세요! |
| `tut.protector_what` | This cell has a shield | 이 셀에는 보호막이 있어요 |
| `tut.protector_how` | Tap a same-color group to strip it | 같은 색 그룹을 탭하면 벗겨져요 |
| `tut.core_warning` | This cell must be removed to clear the stage | 이 셀을 제거해야 스테이지 클리어가 돼요 |
| `tut.obstacle_what` | This cell cannot be removed by tapping | 이 셀은 탭으로 제거할 수 없어요 |
| `tut.item_hint_prompt` | Try using an item! | 아이템을 써보세요! |
| `tut.item_bomb_effect` | Bomb: removes all surrounding cells at once | 폭탄: 주변 셀을 한 번에 제거해요 |
| `tut.rotation_explain` | The board flipped! Gravity follows the new direction | 보드가 뒤집혔어요! 중력이 새 방향으로 적용돼요 |

---

## 11. MVP Scope

### Included
- Phase A: Stage 1 forced guide (5 steps, `is_blocking = true` on step 0)
- Phase A: Stage 2 semi-guide (1 step)
- Phase B: Protector first-encounter hint
- Phase B: Core first-encounter hint
- Phase B: Obstacle first-encounter hint
- Phase C: 3-fail item hint (item available guard)
- `TutorialOverlay` on `Canvas_Overlay (Sort: 20)`
- `SpotlightCutout` with UI (RectTransform) and World coordinate targeting
- Responsive recalculation on `OnRectTransformDimensionsChange`
- `tutorial_step` CSV + `user_tutorial_progress` server record
- Guest PlayerPrefs fallback

### Excluded (Phase 2)
- Phase B: Board rotation hint
- Phase C: Rewarded ad CTA when no items available
- Guest → server tutorial progress migration on account link

---

## 12. Risks

| Risk | Severity | Mitigation |
|------|----------|------------|
| Stage 1 3-touch guarantee not met at ship time | Medium | `verifiedSolution` contract: Editor operator records 3-tap clear sequence before export. stage_id=1 requires explicit QA sign-off |
| SpotlightCutout misaligns on notched devices | Medium | Recalculate on `OnRectTransformDimensionsChange`; QA on iPhone 14+ and Pixel 7 reference devices |
| World-mode spotlight drifts during board load animation | Low | Recalculate every frame until `BoardView.IsAnimating == false` |
| `gimmick_appear` trigger fires before player can see the cell | Low | Delay trigger evaluation by one frame after `OnBoardReady()` to ensure board render completes |
| `user_tutorial_progress` API latency causes duplicate step replay | Low | Client-side cache as source of truth; server response corrects after-the-fact |
