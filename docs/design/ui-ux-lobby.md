# UI/UX — Lobby

## Boot Screen

- Full-screen logo splash, no user interaction
- Background: `UI_BG_DEEP` solid
- Center: pixel-art spinner (loading indicator)
- Target duration: < 3s
- No skip

---

## Lobby Layout

```
┌──────────────────────────┐
│  [Avatar]      [🪙 1,240] │  ← Home 탭만 표시
│                           │
│       [Tab Content]       │
│                           │
│                           │
├───────────────────────────┤
│  🏠 Home  🛒 Shop  🏆 Rank │  ← bottom nav bar
└───────────────────────────┘
```

- Active tab: `UI_CTA` accent highlight
- Bottom Tabs: `🏠 Home`, `🛒 Shop` (disabled/placeholder in MVP), `🏆 Rank` (active!)

---

## Ranking Tab (Global & Stage Leaderboard)

To support leaderboards with thousands of entries without performance degradation:
- **Dynamic Scroll Virtualization (Object Pooling)**:
  - The UI ScrollRect only instantiates enough Ranking Entry prefabs to cover the visible viewport (plus 2 padding rows on top and bottom, typically ~10 rows).
  - As the user scrolls, the view reuse manager shifts the out-of-view row components to the opposite edge and binds the new data index (nickname, avatar, score, rank).
  - Eliminates garbage collection and draw-call spikes during scrolling.

---

## Settings Panel & Sound Integration

Accessed via Settings button. Handles runtime configuration.

### Sound Sliders (BGM & SFX)
- **Controls**: Includes two Slider UI elements for BGM and SFX volume levels (0% to 100%).
- **Interaction**: Dragging the slider sends real-time volume updates to the `SoundManager` singleton and persists the values in `PlayerPrefs` (`bgm_volume` and `sfx_volume`).
- **Mute Toggles**: Checkboxes to quickly mute/unmute BGM or SFX without losing slider settings.

---

## Home Tab — Chapter/Stage Scroll

### Architecture

Infinite vertical scroll. Object pool: ~15 stage nodes active at a time.

```
[Background Layer]       ← per-chapter sprite, crossfade at boundary
[Path Sprite Layer]      ← per-chapter winding path (S-curve/zigzag)
[Stage Node Pool]        ← pooled objects, repositioned as scroll moves
[Chapter Boundary Deco]  ← separator decoration at Y boundary
```

### Stage Node X-Position Pattern (simple zigzag)

```
node_index % 3:
  0 → center
  1 → left offset  (-80px)
  2 → right offset (+80px)

Arc connector sprite links adjacent nodes (pre-authored per 3-node group).
```
Path is casual/simple — no bezier calculation required. Pre-defined X offsets only.

### Chapter Background Transition

- Each chapter owns one background sprite (height covers its scroll range)
- Chapter sprites stacked vertically in background layer — NOT one long image
- Scroll Y enters next chapter's Y range → alpha crossfade between two background sprites (0.5s)
- Chapter boundary deco (decorative divider) placed at exact Y boundary

### Stage Node States

| State | Visual |
|-------|--------|
| Locked | Dark overlay + lock icon |
| Cleared 0★ | Node base, no star fill |
| Cleared 1★ | 1 star filled |
| Cleared 2★ | 2 stars filled |
| Cleared 3★ | 3 stars filled, gold border |
| Current (unlocked, not cleared) | Pulsing ring highlight |

### Chapter Completion Chest

Placed at chapter boundary deco.

| State | Visual |
|-------|--------|
| Locked (not all 3★) | Greyscale chest |
| Claimable (all 3★) | Animated gold glow, tap to claim |
| Claimed | Opened chest, static |

Tap claimable → reward popup → item/gold display.

---

## StageInfo Popup

Tap on any non-locked stage node.

```
┌──────────────────────┐
│  Stage 7              │
│  Best: ★★☆  Moves: 24 │
│                      │
│       [PLAY]          │
└──────────────────────┘
```

- Tap outside popup → dismiss
- [PLAY] → Lobby to InGame transition (slide up)

---

## Shop Tab

MVP: item list placeholder (Bomb, H-Rocket, V-Rocket).
Prices sourced from `shop_item.csv`.
Full economy UI: Phase 2.

---

## Header (Home Tab only)

| Element | Position | Action |
|---------|----------|--------|
| Player avatar | Top-left | Tap → Account popup |
| Gold balance (🪙 + amount) | Top-right | Display only |
