# UI/UX — Scene Structure

## Scene Graph

```
Boot
  └─(auth resolved)──► Lobby
                            └─(tap Play)──► InGame
                                               └─(overlays stay in InGame scene)
```

## Scenes

| Scene | Role | Entry | Exit |
|-------|------|-------|------|
| Boot | Auth check, asset load | App launch | Auto → Lobby |
| Lobby | Home/Shop/Ranking tabs | Boot | — (root) |
| InGame | Board gameplay + result/fail overlays | Lobby (Play tap) | Lobby (Result buttons) |

## Transitions

| From | To | Type | Trigger |
|------|----|------|---------|
| Boot | Lobby | Fade | Auth resolved (guest or authenticated) |
| Lobby | InGame | Slide up | [PLAY] in StageInfo popup |
| InGame | Lobby | Slide down | [Map] in Result / [스테이지 선택] in Pause |
| InGame | InGame | Board reload | [Retry] in Result or Pause |

## InGame Overlays (no scene transition)

| Overlay | Trigger | Dismiss |
|---------|---------|---------|
| ResultOverlay | Stage end (any outcome) | Button tap |
| FailOverlay (Continue) | turns=0 AND ratio < star1 (1회/attempt) | [계속하기] / [포기] |
| PausePopup | Pause button | [재개] / [처음부터] / [스테이지 선택] |

## Lobby Tab Structure

Bottom navigation bar. Icon + label per tab.

| Tab | MVP | Phase 2 |
|-----|-----|---------|
| Home | Chapter/Stage scroll | same + event banners |
| Shop | Item list placeholder | Full economy UI |
| Ranking | Disabled (greyed) | Global star leaderboard |
