# shared/datas/stage

## Files
| file | role |
|------|------|
| `chapter.csv` | Chapter definitions: display order, unlock condition, clear reward, background theme |
| `stage.csv` | Stage definitions: board layout, turn limit, color set, star thresholds, reward group |

## chapter.csv Columns
| column | scope | type | note |
|--------|-------|------|------|
| `chapter_id` | CS | int32 PK | Sequential chapter number |
| `display_order` | C | int8 | Chapter display order in scroll UI |
| `unlock_chapter_id` | CS | int32 nullable | Chapter that must be cleared to unlock this chapter |
| `reward_group_id` | S | int32 | Server chapter-clear reward group |
| `bg_theme_id` | C | int8 | Client background theme ID (maps to shader/particle preset) |

> Chapter display name derived from `chapter.name_fmt` in `client_string.csv` with `chapter_id` as arg.

## stage.csv Columns
| column | scope | type | note |
|--------|-------|------|------|
| `stage_id` | CS | int32 PK | Sequential stage number |
| `chapter_id` | CS | int32 | FK: chapter — owning chapter |
| `stage_order` | C | int8 | Display order within the chapter; allows mid-chapter insertion |
| `board_width` | CS | int8 | Columns; server validates cells length |
| `board_height` | CS | int8 | Rows; server validates cells length |
| `turn_limit` | CS | int8 | Max valid taps; server validates `turns_used` |
| `difficulty` | C | Difficulty | Easy=0 Normal=1 Hard=2 |
| `color_ids` | C | string | Comma-separated palette IDs used in this stage, e.g. `0,1,3,5` |
| `star1_ratio` | CS | float | Server star threshold for 1 star |
| `star2_ratio` | CS | float | Server star threshold for 2 stars |
| `cells` | CS | string | Flat CTM hex string; server counts Basic cells and core cells |
| `verified_solution` | C | string | Tap sequence `row,col;row,col;...` (0-indexed) |
| `ruleset_version` | CS | int8 | Locks client/server validation compatibility |
| `reward_group_id` | S | int32 | Server stage clear reward group |
| `rotation_interval` | CS | int8 | Board rotation interval (0 = disabled) |

## Cell Encoding
```text
C = color_id hex char
T = CellType hex char (0=Basic, 1=Obstacle, 2=Void)
M = modifier hex char bitmask
    bits[1:0] = protector_strength (0=none, 1=1-layer, 2=2-layer)
    bit[2]    = is_core (0/1)
    bit[3]    = reserved
```

Parse: `cells[i*3 .. i*3+3]` for cell index i (row-major: i = row * board_width + col).

## bg_theme_id Convention
| id | intended look |
|----|--------------|
| 1  | Grassland (Easy) |
| 2  | Ocean (Normal) |

Client maps `bg_theme_id` → shader palette + particle preset. No sprite swap required per chapter.

## Rules
- NEVER hand-edit `cells`; use the stage editor.
- `color_ids` must list every color_id that appears in `cells`; editor validates this.
- `star1_ratio` < `star2_ratio` always; star3 = full clear.
- `verified_solution` must be re-recorded whenever board layout changes.
- Server validation depends on all `CS` fields; do not downgrade those scopes without updating `StageAttemptService`.
- `stage_order` controls in-chapter display; use it for boss/special stage insertion without renumbering `stage_id`.
- `unlock_chapter_id` null = always unlocked (Chapter 1); server enforces unlock gate at stage-attempt time.

## Cross-refs
- Consumed by: `Game.Services.StageDataService`
- Consumed by: `ProjectFlood.Application.Stage.StageAttemptService`
- Gen output: `client/Assets/Resources/Data/stage/`
- Gen output: `server/src/ProjectFlood.Domain/StaticData/StageData.g.cs`
- Depends on: `common/color_palette.csv`, `shared/contracts/GameTypes/GameEnums.cs`
