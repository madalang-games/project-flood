# docs/design

UI/UX specs, wireframes, game design documents.

## Files
| file | role |
|------|------|
| `game-design.md` | Core game design document: rules, cell types, clear conditions, MVP scope |
| `stage-editor-design.md` | Stage editor feature spec: UI layout, API routes, playtest, export validation |
| `ingame-core-design.md` | InGame core architecture: modules, tap flow, clear conditions, stage loading |
| `item-system-design.md` | Item system spec: Bomb/H-Rocket/ColorSweep/RowShift/CellSwap, Use Phase UX, Void/Obstacle policy, architecture integration |
| `progression-system-design.md` | Progression system spec: Star logic, Chapter grouping, Milestone rewards |
| `economy-system-design.md` | Game economy spec: currency types, reward formulas, sink/source balance |
| `stamina-system-design.md` | Stamina, rewarded ad, unlimited time, reward source, Redis attempt, and event log spec |
| `social-ranking-design.md` | Social and ranking spec: Redis-based global ranking, stage performance UX |
| `ui-ux-config.md` | Global UI/UX conventions: palette, typography, touch targets, animation, safe area, z-order, pixel art scaling |
| `ui-ux-common-components.md` | Shared UI: ConfirmDialog, Toast, LoadingOverlay, RewardPopup, NetworkError, animation components, Settings panel |
| `ui-ux-canvas-architecture.md` | Canvas hierarchy, Sort Order, Canvas Scaler settings, SafeAreaHandler, UIManager API, responsive policy |
| `ui-ux-scene-structure.md` | Scene graph, transitions, overlay taxonomy, Lobby tab structure |
| `ui-ux-lobby.md` | Boot screen, Lobby layout, Home tab chapter/stage scroll, StageInfo popup, Shop tab |
| `ui-ux-ingame.md` | InGame HUD, Result overlay, Fail overlay, Pause popup |
| `ui-ux-auth.md` | Boot auth sequence, Guest mode, OAuth link flow, account switching, clientLogin |
| `ftue-tutorial-design.md` | FTUE and Tutorial system: onboarding, gimmick hints, fail hints, TutorialManager architecture, tutorial_step data schema |
| `client-settings-system-design.md` | Client settings design spec: localized texts, sound levels, haptics, and local cache policies |

## Rules
- One file per major design area.
- Wireframe images: store in `design/assets/`.
- Do not duplicate content from ADRs; link to relevant `decisions/` entries instead.
