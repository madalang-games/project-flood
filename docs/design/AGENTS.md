# docs/design

UI/UX specs, wireframes, game design documents.

## Files
| file | role |
|------|------|
| `game-design.md` | Core game design document — rules, cell types, clear conditions, MVP scope |
| `stage-editor-design.md` | Stage editor feature spec — UI layout, API routes, playtest, export validation |
| `ingame-core-design.md` | InGame core architecture — modules, tap flow, clear conditions, stage loading |
| `item-system-design.md` | Item system spec — Bomb/H-Rocket/V-Rocket, Use Phase UX, Void/Obstacle policy, architecture integration |
| `progression-system-design.md` | Progression system spec — Star logic, Chapter grouping, Milestone rewards (Chests) |
| `economy-system-design.md` | Game economy spec — Currency types, reward formulas, sink/source balance |
| `social-ranking-design.md` | Social & Ranking spec — Redis-based global ranking, stage performance % UX |

## Rules
- One file per major design area (e.g., `ui-design.md`, `game-design.md`, `onboarding.md`)
- Wireframe images → store in `design/assets/`
- Do not duplicate content from ADRs — link to relevant `decisions/` entries instead
