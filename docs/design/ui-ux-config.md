# UI/UX Config — Global Conventions

Industry-standard baseline. All UI implementation uses this file as SoT.

---

## 1. Color Palette

### UI Colors (8 tokens)

| Token | Hex | Usage |
|-------|-----|-------|
| `UI_BG_DEEP` | `#0D1B2A` | Panel backgrounds, overlay dim layer |
| `UI_BG_MID` | `#1A2F45` | Card surfaces, popup inner backgrounds |
| `UI_BORDER` | `#2A4A6B` | Panel borders, dividers |
| `UI_PRIMARY` | `#4A90D9` | Primary action buttons (Play, Confirm) |
| `UI_CTA` | `#E8A020` | Gold-spend / purchase buttons, stars, coin icons |
| `UI_SUCCESS` | `#3DBE6E` | Clear feedback, positive states, ratio bar above threshold |
| `UI_DANGER` | `#E84040` | Fail states, warnings, ratio bar below threshold, insufficient gold |
| `UI_TEXT` | `#F0EAD6` | Default text |

### Game Cell Palette (16 colors) → see `game-design.md` §3. Never mix with UI tokens.

### State Color Rules

| State | Treatment |
|-------|-----------|
| Disabled | Base color × 40% opacity |
| Pressed / Active | Base color × 80% brightness (darken) |
| Hover | Not applicable (mobile) |

---

## 2. Typography

### Font Selection

| Use | Font | Constraint |
|-----|------|------------|
| Large headers (stage number, result title) | Pixel font — `m5x7` (free, readable) | 24px and above only |
| Body, labels, button text | Readable sans-serif — `Noto Sans` (free, multilingual) | All sizes |
| Emphasized numbers (turns, gold, ratio) | Pixel font or Bold sans-serif | Large, bold |

**Never use pixel font below 20px — unreadable on mobile.**

### Size Scale (dp)

| Role | Size | Weight |
|------|------|--------|
| Screen title | 28–32dp | Bold |
| Section header | 20–24dp | Bold |
| Button text | 18–20dp | Bold |
| Body / label | 16–18dp | Regular |
| Secondary info (best record, etc.) | 14dp | Regular |
| Minimum allowed | **12dp** | — |

Absolute minimum: **12dp**. Nothing smaller on mobile.

---

## 3. Touch Targets

| Rule | Value |
|------|-------|
| Minimum touch area | **48×48dp** (Google Material standard) |
| Primary action buttons (Play, Confirm, Retry) | **56×56dp** recommended |
| Minimum gap between adjacent tappable elements | **8dp** |

Visual size may be smaller than 48dp — extend hit area via `EventTrigger` or `IPointerClickHandler` with a larger RectTransform.

---

## 4. Button Feedback (Affordance)

Required on every tappable element. Missing feedback causes repeated taps and UX frustration.

| Event | Effect | Duration |
|-------|--------|----------|
| Press down | Scale × 0.95 | 80ms ease-in |
| Release | Scale × 1.0 | 80ms ease-out |
| Disabled | Opacity 40%, touch blocked | — |

---

## 5. Animation Timing

### Standard Durations

| Type | Duration | Easing |
|------|----------|--------|
| Scene transition (fade / slide) | 250ms | ease-in-out |
| Popup / overlay appear | 200ms | ease-out |
| Popup / overlay disappear | 150ms | ease-in |
| Button feedback | 80ms | ease-in-out |
| Star pop animation | 300ms / star | ease-out-back (slight overshoot) |
| Gold count-up | 600ms | ease-out |
| Dim background | 200ms | linear |
| In-scene panel slide | 200ms | ease-out |

### Easing Guide
- `ease-out`: fast start → slow end. Use for enter animations.
- `ease-in`: slow start → fast end. Use for exit animations.
- `ease-out-back`: ease-out + slight overshoot. Use for star / reward pops (springy feel).

**Under 200ms: users miss it. Over 500ms: feels sluggish. Stay within this range.**

---

## 6. Spacing System

8dp grid (Google Material standard). All values must be multiples of 4.

| Value | Use |
|-------|-----|
| 4dp | Icon ↔ label gap, tight inner padding |
| 8dp | Default element spacing |
| 16dp | Panel inner padding, intra-section spacing |
| 24dp | Section separation |
| 32dp | Large area division |

---

## 7. Safe Area

| Region | Rule |
|--------|------|
| Top (notch / Dynamic Island) | All HUD elements start below safe area top edge |
| Bottom (home bar, iPhone X+) | Bottom nav bar aligns to safe area bottom edge |
| Left / Right | Portrait-only game — no lateral safe area adjustment needed |
| Background images | Edge-to-edge bleed allowed (non-interactive) |

**Unity implementation:** `Screen.safeArea` API → adjust `RectTransform` anchors. Without this, buttons may be obscured by notch or home bar.

Screen orientation: **Portrait locked.** Matches `game-design.md §2` portrait-optimized design.

---

## 8. Z-Order / Canvas Layer Stack

Back → Front.

| Layer | Contents | Unity Sort Order |
|-------|----------|-----------------|
| World Background | Chapter background sprites | 0 |
| Game Board | Board, cells | 10 |
| Board Effects | Cell removal FX, gravity animation | 20 |
| HUD | Turn counter, ratio bar, buttons | 30 |
| Overlay Dim | Semi-transparent dark background | 40 |
| Overlay Content | Result, Fail, Pause panels | 50 |
| Popup | Confirm dialog, StageInfo | 60 |
| Toast | Transient notification messages | 70 |
| Loading | Loading overlay, full-screen transitions | 100 |

Rendering a popup below HUD sort order causes buttons to hide behind the board. This order is mandatory.

---

## 9. Pixel Art Scaling Rules

| Item | Value |
|------|-------|
| Reference resolution | 1080×1920 (Full HD Portrait) |
| Canvas Scaler | Scale With Screen Size, Ref 1080×1920, Match = 0.5 |
| Sprite filtering | **Point (no filter)** — Bilinear/Trilinear blurs pixel art |
| Cell base size | 48×48px |
| Scale increments | Integer multiples only (1×, 2×, 4×). Fractional scale breaks pixel grid. |
| Art production base | 48×48px source → upscale to 2× (96×96) if needed |

9×11 board: 9×48 = 432px wide, 11×48 = 528px tall — adequate margin at 1080p.

---

## 10. 9-Slice Panel / Button Rules

| Element | Source sprite size | Border (all sides) |
|---------|-------------------|--------------------|
| Standard panel | 64×64px | 12px |
| Popup panel | 96×96px | 16px |
| Button | 48×24px | 10px H / 8px V |
| Progress bar background | 32×16px | 4px H / 0px V |
| Progress bar fill | 16×16px | 0 (stretch center only) |

Border area: fixed (corners). Center: must be solid color or repeating pattern for clean stretching.

---

## 11. Color Contrast (Accessibility Minimum)

| Combination | Ratio | Result |
|-------------|-------|--------|
| `UI_TEXT` on `UI_BG_DEEP` | ~14:1 | WCAG AAA ✓ |
| `UI_TEXT` on `UI_BG_MID` | ~10:1 | WCAG AAA ✓ |
| `UI_PRIMARY` on `UI_BG_DEEP` | ~4.6:1 | WCAG AA ✓ |
| `UI_DANGER` on `UI_BG_DEEP` | ~5.2:1 | WCAG AA ✓ |

Minimum: **WCAG AA (4.5:1)**. This palette meets the requirement.
Icons and decorative elements with no text: contrast requirement does not apply.
