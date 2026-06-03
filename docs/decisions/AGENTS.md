# docs/decisions — Architecture Decision Records

NEVER delete ADR files. Supersede by setting `Status: superseded-by ADR-XXX`.

## Files
| file | date | status | summary |
|------|------|--------|---------|
| `ADR-001-gimmick-cells-removed-items-only.md` | 2026-06-04 | accepted | Bomb/HRocket/VRocket: board cells removed, items only |
| `ADR-002-protector-rule-redesign.md` | 2026-06-04 | accepted | Protector: Basic only, direct-hit strip, 1–2 layers |
| `ADR-003-cell-ctm-hex-encoding.md` | 2026-06-04 | accepted | Stage cell encoding: CTM hex, 3 chars/cell, no separator |
| `ADR-004-isolated-cell-tap-allowed.md` | 2026-06-04 | accepted | Size=1 group tap valid; turn consumed; no permanent stuck state |
| `ADR-005-stage-editor-nextjs-service.md` | 2026-06-04 | accepted | Stage editor: standalone Next.js service, not Unity Editor tool |
| `ADR-006-ingame-architecture.md` | 2026-06-04 | accepted | Pure C# rule engine + MonoBehaviour view separation |

## Rules
- Filename: `ADR-NNN-kebab-case-title.md` (zero-padded 3-digit number)
- One ADR per significant decision — tech stack choices, data model choices, cross-cutting patterns
- When superseding: update old ADR status + create new ADR explaining the change
- When adding a new ADR: update the Files table above
