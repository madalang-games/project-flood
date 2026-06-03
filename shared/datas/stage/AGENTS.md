# shared/datas/stage

## Files
| file | role |
|------|------|
| `stage.csv` | Stage definitions — board layout, turn limit, color set, star thresholds |

## CSV Columns
| column | scope | type | note |
|--------|-------|------|------|
| `stage_id` | CS | int32 PK | sequential stage number |
| `board_width` | C | int8 | columns |
| `board_height` | C | int8 | rows |
| `turn_limit` | C | int8 | max valid taps |
| `difficulty` | C | [Difficulty] | Easy=0 Normal=1 Hard=2 |
| `color_ids` | C | string | comma-separated palette IDs used in this stage e.g. `0,1,3,5` |
| `star1_ratio` | C | float | clearance ratio threshold for 1★ (default 0.80) |
| `star2_ratio` | C | float | clearance ratio threshold for 2★ (default 0.90) |
| `cells` | C | string | flat CTM hex string, row-major, no separator |
| `verified_solution` | C | string | tap sequence `row,col;row,col;...` (0-indexed) |
| `ruleset_version` | C | int8 | locks replay fidelity for verified_solution |

## Cell Encoding (CTM hex, 3 chars/cell)
```
C = color_id   hex char (0–F) → color_palette.color_id
T = CellType   hex char (0=Basic, 1=Obstacle, 2–F reserved)
M = modifier   hex char (bitmask)
    bits[1:0] = protector_strength (0=none, 1=1-layer, 2=2-layer)
    bit[2]    = is_core (0/1)
    bit[3]    = reserved
```
Parse: `cells[i*3 .. i*3+3]` for cell index i (row-major: i = row * board_width + col)

## Rules
- NEVER hand-edit `cells` — use the stage editor
- `color_ids` must list every color_id that appears in `cells`; editor validates this
- `star1_ratio` < `star2_ratio` always; star3 = full clear (no configurable ratio needed)
- `verified_solution` must be re-recorded whenever board layout changes

## Cross-refs
- Consumed by: `client/Assets/Scripts/` (IStaticDataService.GetStage)
- Gen output: `client/Assets/Resources/Data/stage/`
- Depends on: `common/color_palette.csv`, `shared/contracts/GameTypes/GameEnums.cs`
