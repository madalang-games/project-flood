# shared/datas/ad

## Files
| file | class | role |
|------|-------|------|
| `ad_placement.csv` | `AdPlacement` | Rewarded-ad placement definitions |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `AdPlacement.placement_id` | column | Stable ad placement id |
| `AdPlacement.context_type` | column | Expected reward context, e.g. `stage_attempt` |

## Rules
- Ad reward transaction storage is DB-owned; this table only describes placement policy.
- Stamina life and stage revive placements remain generic ad placements, not separate ad systems.

## Cross-refs
- Consumed by: `Server.AdRewardService`
- Consumed by: `Client.AdService`
