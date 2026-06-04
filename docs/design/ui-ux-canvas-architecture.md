# UI/UX — Canvas Architecture

## Overview

Hybrid model: DDOL UIManager (persistent) + per-scene Canvas (scene-specific UI).

```
[UIManager — DontDestroyOnLoad]
  ├── Canvas_Popup    (Sort: 10)
  ├── Canvas_Overlay  (Sort: 20)
  ├── Canvas_Toast    (Sort: 30)
  └── Canvas_Loading  (Sort: 100)

[Boot Scene]
  └── Canvas_Scene    (Sort: 0)

[Lobby Scene]
  └── Canvas_Scene    (Sort: 0)

[InGame Scene]
  └── Canvas_Scene    (Sort: 0)
```

---

## DDOL UIManager Canvas Layers

| Canvas | Sort Order | Contents | Instance Method |
|--------|-----------|----------|----------------|
| Canvas_Popup | 10 | ConfirmDialog, StageInfo, AccountPopup, SettingsPanel, RewardPopup | Dynamic Instantiate/Destroy |
| Canvas_Overlay | 20 | ResultOverlay, FailOverlay, PausePopup, ChapterUnlockOverlay, TutorialOverlay | Dynamic Instantiate/Destroy |
| Canvas_Toast | 30 | Toast | Static instance, Show/Hide |
| Canvas_Loading | 100 | LoadingOverlay, NetworkError | Static instance, Show/Hide |

**Stacking rules:**
- Overlay: one at a time. New overlay request closes existing one first.
- Popup: stackable. ConfirmDialog can stack on top of SettingsPanel.
- Back gesture: dismisses top-most Popup. Overlays close via buttons only.

---

## Per-Scene Canvas Contents

| Scene | Canvas_Scene contents |
|-------|----------------------|
| Boot | Logo image, loading spinner |
| Lobby | Header (Avatar, Gold), BottomNavBar, TabContent (Home/Shop/Ranking) |
| InGame | HUD (Pause button, TurnCounter, RatioBar), BoardContainer anchor |

Scene UI always renders at Sort Order 0 — always behind UIManager canvases (10–100).

---

## Canvas Scaler — Identical Settings on ALL Canvases (required)

| Property | Value |
|----------|-------|
| UI Scale Mode | Scale With Screen Size |
| Reference Resolution | 1080 × 1920 |
| Screen Match Mode | Match Width Or Height |
| Match | 0.5 |
| Reference Pixels Per Unit | 100 |

Mismatch between any canvases causes cross-layer size inconsistency.

---

## SafeAreaHandler Component

Handles notch / Dynamic Island / home bar (iPhone X+). Attach to edge-adjacent UI containers.

**Attach targets:**

| Element | Reason |
|---------|--------|
| Lobby Header container | Respect top safe area |
| BottomNavBar container | Respect bottom safe area |
| InGame HUD container | Respect top safe area |
| DDOL Canvas_Loading panel | Full-screen overlay needs top + bottom |

Background images: edge-to-edge bleed is allowed (non-interactive). No SafeAreaHandler needed.

**Implementation:**
```csharp
void ApplySafeArea() {
    Rect safe = Screen.safeArea;
    Vector2 screenSize = new Vector2(Screen.width, Screen.height);
    rt.anchorMin = safe.position / screenSize;
    rt.anchorMax = (safe.position + safe.size) / screenSize;
}
// Call on both OnEnable and OnRectTransformDimensionsChange
```

---

## UIManager API (behavioral spec)

```
UIManager.ShowPopup<T>(params)      → instantiate T on Canvas_Popup, return instance
UIManager.ShowOverlay<T>(params)    → instantiate T on Canvas_Overlay (closes existing overlay first)
UIManager.ShowToast(msg, type)      → activate Toast on Canvas_Toast
UIManager.ShowLoading()             → activate LoadingOverlay on Canvas_Loading
UIManager.HideLoading()             → deactivate LoadingOverlay
UIManager.ShowNetworkError(onRetry) → activate NetworkError, bind retry callback
UIManager.CloseTopPopup()           → destroy top-most item on Canvas_Popup
```

---

## Responsive Inner Elements

### TMP FontSize Policy

| Case | Setting |
|------|---------|
| Fixed headers, button labels, numbers | **Fixed size.** Canvas Scaler handles device scaling. |
| Dynamic content (player names, item descriptions) | TMP Enable Auto Sizing: min 12dp, max = designed size |

Never apply Auto Sizing to text that drives layout (parent has ContentSizeFitter) — causes layout instability.

### RectTransform Sizing Policy

| Pattern | Unity Component | Use Case |
|---------|----------------|---------|
| Width driven by text length | ContentSizeFitter (Horizontal Fit) | Dynamic labels, tags |
| Height driven by line count | ContentSizeFitter (Vertical Fit) | Tooltips, description boxes |
| List / grid items | VerticalLayoutGroup / GridLayoutGroup | Stage nodes, reward item list |
| Fill remaining space | LayoutElement (flexibleWidth/Height = 1) | Spacers, stretch containers |
| Maintain square aspect | AspectRatioFitter | Cell icons, avatars |

Fixed-size panels (popups, HUD): do not apply ContentSizeFitter. Overflow content → handle with ScrollRect.

### Supported Aspect Ratios

Canvas Scaler Match 0.5 coverage:

| Ratio | Example devices | Supported |
|-------|----------------|-----------|
| 16:9 | Older Android | ✓ baseline |
| 18:9 – 20:9 | Most modern phones | ✓ automatic |
| 19.5:9 (Dynamic Island) | iPhone 14+ | ✓ requires SafeAreaHandler |
| 4:3 (tablet) | iPad | Layout drift possible — not supported in MVP |

Tablet support: Phase 2, separate LayoutProfile branch.
