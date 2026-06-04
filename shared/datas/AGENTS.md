# shared/datas - Game Meta Data (CSV Sources)

## Nav
| path | domain |
|------|--------|
| `stage/` | Stage/level definitions |
| `currency/` | Currency type definitions |
| `item/` | Item metadata |
| `reward/` | Reward group and entry tables referenced by FK from other domains |
| `ad/` | Rewarded-ad placement config |
| `shop/` | Shop catalog entries |
| `stamina/` | Stamina config |
| `ranking/` | Ranking season config |
| `event/` | Event definitions, one file per event type |
| `achievement/` | Achievement definitions |
| `tutorial/` | Tutorial step definitions |
| `string/` | Localization strings, one file per language |
| `common/` | Shared enums, difficulty labels, other lookup tables |

## CSV Format
```
Row 1: field names
Row 2: target scope - C | S | CS
Row 3: normalized type - int8/16/32/64, uint8/16/32/64, float, double, bool, string, string(N), EnumName
Row 4: constraints - PK, FK:[table], NN, UQ, IDX, AUTO
Row 5+: data
```

## Output (after `npm run gen:info`)
- `client/project-flood/Assets/Resources/Data/`
- `server/generated/data/`
- `server/generated/scripts/*/Xxx.g.cs`
- `client/.../Data/Generated/`
- `IStaticDataService.g.cs`
- `StaticDataService.g.cs`

## Normalization Rules
- 1 CSV = 1 entity type; no multi-entity tables.
- Rewards by reference: use `reward_group_id`; never inline reward columns.
- Event tables split by type; never use `event_type` column to mix types.
- Enums in `common/`; all enum/label tables live there.
- FK naming: always `{entity}_id`, e.g. `reward_group_id`, `item_id`.
- `_` prefix files/dirs are skipped by all generators.

## Cross-refs
- Gen output: `client/project-flood/Assets/Resources/Data/`, `server/generated/data/`
- Consumed by: `ProjectFlood.Infrastructure.StaticData.StaticDataService`
- Consumed by: `client/project-flood/Assets/Scripts/Services/`
