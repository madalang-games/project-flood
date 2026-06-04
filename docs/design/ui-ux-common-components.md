# UI/UX — Common Components

Reusable components shared across all scenes.

---

## 1. Generic Confirm Dialog

Used for: quit confirm, account switch warning, restart confirm — any binary decision.

```
[dim overlay]
┌──────────────────────┐
│  [Title]              │
│                      │
│  [Body message]       │
│                      │
│  [Cancel] [Confirm]  │
└──────────────────────┘
```

| Property | Value |
|----------|-------|
| Title | Injected by caller |
| Body | Injected by caller (optional; hidden if empty) |
| Cancel label | Default "Cancel"; caller can override |
| Confirm label | Injected by caller |
| Confirm button color | Default `UI_PRIMARY`; destructive actions use `UI_DANGER` |
| Tap outside | Dismiss = Cancel behavior |
| Animation | Panel scale 0.85→1.0 + alpha 0→1 (200ms ease-out) |

---

## 2. Toast / Snackbar

Used for: insufficient gold, network error, lightweight feedback. Auto-dismiss.

```
┌───────────────────────────┐
│  ⚠  Insufficient gold      │
└───────────────────────────┘
```

| Property | Value |
|----------|-------|
| Position | Bottom of screen, above bottom nav bar, within safe area |
| Display duration | 2.5s |
| Appear | Slide up + alpha 0→1 (150ms) |
| Dismiss | Alpha 1→0 (200ms) |
| Icon | By type: ⚠ warning / ✓ success / ✕ error |
| Stacking | Replaces existing toast (no queue) |
| Tap | Instant dismiss |

---

## 3. Loading Overlay

Used for: server response wait, scene transition, account link in progress.

```
[full-screen dim 0.6 opacity]
        [pixel-art spinner]
      [optional message text]
```

| Property | Value |
|----------|-------|
| Background | `UI_BG_DEEP` 60% opacity |
| Spinner | Pixel-art 8-frame rotation, 48×48dp |
| Message | Optional; hidden by default |
| Touch block | Full-screen Raycast Block |
| Appear / dismiss | Alpha fade 150ms |
| Timeout | 10s → auto-dismiss + show NetworkError |

---

## 4. Reward Popup

Used for: chapter chest claim, special reward grants.

```
[dim overlay]
┌──────────────────────┐
│    Reward!            │
│                      │
│  [item icons]        │  ← icon + quantity per reward
│  🪙 × 500            │
│  💣 × 3              │
│                      │
│       [OK]           │
└──────────────────────┘
```

| Property | Value |
|----------|-------|
| Reward items | Dynamic binding, max 4 items displayed |
| Icon appear | Sequential pop: scale 0→1.2→1.0, 0.15s gap between items |
| Gold | Count-up animation (600ms ease-out) |
| [OK] | Close popup |
| Tap outside | Ignored (prevents accidental close) |

---

## 5. Network Error UI

Used for: LoadingOverlay timeout, API failure.

```
[dim overlay]
┌──────────────────────┐
│  Unable to connect    │
│                      │
│  Check your network  │
│  connection.         │
│                      │
│      [Retry]         │
└──────────────────────┘
```

| Property | Value |
|----------|-------|
| [Retry] | Re-attempt failed request |
| Tap outside | Ignored |
| Consecutive failures | 3+ → replace message with "Please try again later." |

---

## 6. UI Animation Components

### UIButtonAnimator

Attach to every button. Handles Press / Release / Idle states.

| Event | Effect | Duration | Easing |
|-------|--------|----------|--------|
| Press down | scale × 0.92 | 80ms | ease-in |
| Release | scale × 1.05 → 1.0 | 60ms + 80ms | ease-out → ease-in-out |
| Idle (CTA only) | scale 1.0 → 1.04 → 1.0 loop | 2.5s period | sine |
| Disabled | opacity 40%, animation stopped | — | — |

CTA idle breathing: only on `UI_CTA`-colored buttons. Standard buttons have no idle animation.

---

### UIFloatAnimation

Attach to icons / objects. Gentle vertical float loop.

| Parameter | Default | Notes |
|-----------|---------|-------|
| amplitude | 4dp | Vertical travel distance |
| period | 3.0s | One full cycle |
| easing | sine | Natural float feel |
| randomOffset | true | Randomizes phase so multiple icons don't move in sync |

Used on: gold coin icon, claimable chapter chest, unlocked stage node.

---

### UIScalePulse

Attach to elements requiring visual attention. Breathing scale loop.

| Parameter | Default |
|-----------|---------|
| minScale | 1.0 |
| maxScale | 1.12 |
| period | 1.2s |
| easing | ease-in-out |

Used on: current playable stage node ring, claimable chest.

---

### UIPanelAppear / Disappear

Standard appear/dismiss animation. Apply to all popups and overlays.

| Direction | Scale | Alpha | Duration | Easing |
|-----------|-------|-------|----------|--------|
| Appear | 0.85 → 1.0 | 0 → 1 | 200ms | ease-out |
| Disappear | 1.0 → 0.9 | 1 → 0 | 150ms | ease-in |

---

### UICountUp

Animates a number from 0 to target value.

| Parameter | Default |
|-----------|---------|
| duration | 600ms |
| easing | ease-out |
| formatString | `{0:N0}` (thousands separator) |

Used on: gold earned display, result screen clearance ratio.

---

### UIStarPop

Dedicated to result screen star appearance.

```
scale 0 → 1.3 → 1.0  +  particle burst
```

| Parameter | Value |
|-----------|-------|
| duration | 350ms |
| easing | ease-out-back |
| particle | 8-direction sparkle burst, `UI_CTA` color |
| delay per star | 400ms after previous star completes |

Unfilled (grey) stars: appear instantly, no animation.

---

## 7. Prefab Folder Structure

```
Assets/
└── UI/
    ├── Prefabs/
    │   ├── Base/           ← Generator output only. Never edit manually.
    │   │   ├── Base_Button.prefab
    │   │   ├── Base_Panel.prefab
    │   │   └── Base_Text.prefab
    │   ├── Final/          ← Prefab Variants. Manual customization here.
    │   │   ├── Btn_Play.prefab
    │   │   ├── Btn_Retry.prefab
    │   │   ├── Panel_Result.prefab
    │   │   ├── Panel_StageInfo.prefab
    │   │   └── ...
    │   └── Common/         ← Hand-crafted. Generator does not touch.
    │       ├── ConfirmDialog.prefab
    │       ├── Toast.prefab
    │       ├── LoadingOverlay.prefab
    │       ├── RewardPopup.prefab
    │       └── NetworkError.prefab
    └── Scenes/
        ├── Boot.unity
        ├── Lobby.unity
        └── InGame.unity
```

`Common/` does not need Variant structure — generator never writes here.

---

## 8. LocalizedText / TMPro Material Policy

| Operation | Method | Variant safe? |
|-----------|--------|--------------|
| Apply TMP material globally | Change Material property on BasePrefab TMP component | ✓ Safe |
| Add LocalizedText | Add as companion MonoBehaviour alongside TMP (do NOT remove TMP) | ✓ Safe |
| Replace TMP with LocalizedText (TMP removed) | Forbidden — breaks Variant TMP references | ✗ Unsafe |

`LocalizedText.cs` finds `TMP_Text` via GetComponent and sets `.text`. The TMP component itself stays on the BasePrefab permanently.

---

## 9. Settings Panel

Entry points: Lobby Header ⚙ button / InGame Pause popup [Settings] button.
Display: bottom sheet slide-up panel. No scene transition → Canvas_Popup layer.

```
[dim overlay]
┌──────────────────────┐  ← slide up from bottom (250ms ease-out)
│  Settings             │
│  ─────────────────   │
│  BGM          [● ○]  │
│  SFX          [● ○]  │
│  Screen Shake [● ○]  │
│                      │
│  [Account          →]│  → opens Account Popup
│                      │
│  v1.0.0              │
└──────────────────────┘
```

| Property | Value |
|----------|-------|
| Appear | Slide up + alpha 0→1 (250ms ease-out) |
| Dismiss | Slide down + alpha 1→0 (200ms ease-in) |
| Tap outside | Dismiss |
| Toggle state | Stored in PlayerPrefs, restored on app restart |

Pause popup updated button order: [Resume] [Restart] [Settings] [Stage Select]

---

## 10. UIScreenShake

Device vibration replaced with Camera/Canvas shake for impact feedback.
Apply to Canvas_InGame root RectTransform or Camera.

| Level | Amplitude | Duration | Oscillations | Triggers |
|-------|-----------|----------|-------------|---------|
| Medium | 6dp | 200ms | 3 | Bomb/Rocket explosion, Core Cell destroyed |
| Heavy | 10dp | 350ms | 4 | Stage fail |

Button tap: no shake — UIButtonAnimator scale bounce is sufficient.
Implementation: sine curve oscillation on X/Y offset; guaranteed return to origin on complete.

---

## 11. PerfectClear Effect (3-star only)

Plays on Canvas_Overlay after ResultOverlay appears and all 3 star pops complete.

```
Above Result Overlay content (higher sibling order within Canvas_Overlay):
  1. "Perfect!" text
     scale 0 → 1.3 → 1.0  (400ms ease-out-back)
  2. Confetti particles around text
     8-direction radial burst, rotate while falling, duration 2.0s
  3. Text idle wobble
     ±3° rotation loop, period 1.0s (sustains during particles)
```

| Property | Value |
|----------|-------|
| Trigger | star_count == 3 (early clear) |
| Particle colors | `UI_CTA` + `UI_SUCCESS` mix |
| Interaction | Result buttons remain active; tap skips effect |

---

## 12. ChapterUnlock Animation

Full-screen exclusive sequence on new chapter unlock. Interaction blocked until complete.

```
Timeline (~2.7s total):
  0.0s  Dim fade-in (200ms)
  0.2s  Chapter card fly-in from bottom (400ms ease-out)
  0.6s  "Chapter N Unlocked!" text pop (300ms ease-out-back)
  0.9s  Fanfare particles (1.0s)
  1.9s  Hold (500ms)
  2.4s  Fade-out (300ms)
  2.7s  Overlay removed → interaction restored
```

| Property | Value |
|----------|-------|
| Canvas layer | Canvas_Overlay (Sort: 20) |
| Interaction block | Full Raycast Block + GraphicRaycaster disabled |
| Skip | Not allowed — forced playback to completion |
| Trigger | Stage clear causes first stage of next chapter to unlock |

---

## 13. Tutorial / Onboarding System

### Data Structure

**tutorial_step table (CSV → info_generator)**

| Column | Type | Notes |
|--------|------|-------|
| id | INT PK | |
| trigger_type | ENUM | `first_launch` / `stage_clear` / `gimmick_appear` / `chapter_clear` |
| trigger_value | VARCHAR | Value per trigger_type (stage_id, gimmick type, etc.). NULL for first_launch. |
| step_index | INT | Order within same trigger group (0-based) |
| content_type | ENUM | `finger_overlay` / `tooltip` / `highlight_only` |
| target_ui_id | VARCHAR | UI element ID to highlight. Empty = full-screen dim. |
| text_key | VARCHAR | Localization key for display text |

**Server: user_tutorial_progress**

| Column | Type |
|--------|------|
| user_id | INT FK |
| tutorial_id | INT FK → tutorial_step.id |
| viewed_at | DATETIME |

### Display Behavior

| content_type | Visual |
|-------------|--------|
| finger_overlay | Animated hand tap loops over target_ui_id element |
| tooltip | Speech bubble next to target_ui_id + text |
| highlight_only | target_ui_id brightened, rest dimmed |

Interaction rule: only target_ui_id element is tappable; all others Raycast-blocked.
Progression: advances by step_index order. Records viewed_at on server after final step.
No repeat: if tutorial_id exists in user_tutorial_progress, skip.

### Minimum Tutorial Set (MVP)

| Trigger | Content |
|---------|---------|
| first_launch | Stage 1 entry → first tap guide (finger_overlay on board) |
| gimmick_appear(protector) | First stage with Protector cell → tooltip explanation |
| gimmick_appear(core) | First stage with Core cell → tooltip explanation |

---

## 14. App Background / Resume Behavior

| Situation | Behavior |
|-----------|----------|
| Home button / app switch during InGame | No pause. Board state kept in memory. |
| App returns to foreground | Resume immediately. No popup. |
| App fully killed, relaunch | Start from Lobby (scene reload). Board not restored. |
| Board state recovery needed? | No — player retries the stage. |

Detected via `OnApplicationPause(bool)` Unity callback. No need to distinguish full kill (Lobby restart is the default).

---

## 15. Scroll Position Restore

Auto-restore scroll to last played stage node on InGame → Lobby return.

| Item | Detail |
|------|--------|
| Save timing | Before InGame scene loads (on stage tap) |
| Storage | `ScrollStateCache` (session memory — no PlayerPrefs needed) |
| Restore timing | Lobby Home tab OnEnable |
| Restore method | Set `ScrollRect.verticalNormalizedPosition`, or compute node Y from last stage_id and jump immediately (no animation) |
