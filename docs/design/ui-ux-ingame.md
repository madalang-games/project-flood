# UI/UX — InGame

## HUD Layout

```
┌──[⏸]────[Turns: 18]────[████████░░ 2★]──┐
│                                           │
│                [Board]                    │
│                                           │
└───────────────────────────────────────────┘
```

| Element | Position | Notes |
|---------|----------|-------|
| Pause button | Top-left | → PausePopup |
| Remaining turns | Top-center | Large pixel font, counts down |
| Clearance ratio bar | Top-right | 1★ marker at 80%, 2★ at 90%; fill: `UI_SUCCESS` above star1, `UI_DANGER` below |

Gold balance: **NOT shown in InGame HUD.** Shown only in FailOverlay.

---

## Result Overlay

Trigger: stage end (turn exhausted OR all valid cells cleared).

```
[dim background]
┌──────────────────────┐
│     Stage 7 Clear     │
│                      │
│    ★    ★    ☆        │  ← sequential pop: 0.3s per star
│                      │
│  Cleared: 91%        │
│  Turns used: 18/25   │
│  Gold earned: +120   │
│                      │
│  [Retry] [Next] [Map] │
└──────────────────────┘
```

- Stars animate in sequence (left→right, 0.3s each)
- [Next] hidden when next stage is locked
- [Map] → Lobby (slide down)
- Phase 2 addition: "You cleared faster than **X%** of players!" below stats

### Fail Result (ratio < star1, no continue used or declined)

Same overlay, 0 stars filled, no Gold earned line.

---

## Fail Overlay (Continue Popup)

Trigger: `turns == 0 AND clearance_ratio < star1_ratio`
Limit: 1회/stage attempt. If already used → skip directly to Result Overlay.

```
[dim background]
┌──────────────────────┐
│      조금만 더!        │
│                      │
│     +3 Turns         │
│  Cost:  🪙 150        │
│  Owned: 🪙 320        │  ← 보유 골드, Fail Overlay에서만 노출
│                      │
│  [계속하기]  [포기]    │
└──────────────────────┘
```

| State | [계속하기] button |
|-------|----------------|
| Gold ≥ cost | Active |
| Gold < cost | Disabled + "골드 부족" label |

- [계속하기] → deduct Gold, add 3 turns, resume board
- [포기] → Result Overlay (fail, 0 stars)

---

## Pause Popup

Trigger: Pause button (top-left HUD).

```
┌────────────────┐
│   일시정지      │
│  [재개]         │
│  [처음부터]     │
│  [스테이지 선택] │
└────────────────┘
```

- [처음부터] → board reload (same scene, same stage)
- [스테이지 선택] → Lobby (slide down)
