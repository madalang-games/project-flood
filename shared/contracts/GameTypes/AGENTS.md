# shared/contracts/GameTypes

## Files
| file | class | role |
|------|-------|------|
| `GameEnums.cs` | `CellType`, `Difficulty` | Shared game-type enums used in stage CSV and game logic |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `CellType.Basic` | enum value | 0x0 — normal colored cell |
| `CellType.Obstacle` | enum value | 0x1 — non-interactive, excluded from clearance ratio |
| `Difficulty.Easy` | enum value | 0 |
| `Difficulty.Normal` | enum value | 1 |
| `Difficulty.Hard` | enum value | 2 |

## Rules
- `CellType` values 0x2–0xF are reserved for future gimmick board cells (see ADR-001, ADR-003)
- Do NOT add modifier state (protector, core) to `CellType` — modifiers live in the CTM M-byte
- Referenced in `stage.csv` Row 3 as `[CellType]` and `[Difficulty]`

## Cross-refs
- Consumed by: `client/Assets/Scripts/` (stage loader), `server/` (stage_id FK only in MVP)
- Depends on: ADR-001 (gimmick cells decision), ADR-003 (CTM encoding)
