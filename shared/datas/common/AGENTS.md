# shared/datas/common

## Files
| file | role |
|------|------|
| `color_palette.csv` | Master color palette — 16 predefined colors (color_id 0–15) |
| `dynamic_resource.csv` | Dynamic sprite resources (Items, Cells, Sockets, Chests, Toasts, Avatars, UI assets) |

## Rules
- `color_palette.csv` has exactly 16 rows (color_id 0–15); do not reorder or delete rows
- RGB values are placeholder until finalized by art; do not change without art sign-off
- Palette is expandable (append rows beyond 15) without schema change, but CTM C-digit is hex (max F=15); exceeding 16 colors requires encoding change (see ADR-003)

## Cross-refs
- Consumed by: stage editor (color picker), `client/Assets/Scripts/` (cell renderer)
- Gen output: `client/Assets/Resources/Data/common/`
