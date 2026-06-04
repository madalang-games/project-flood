# shared/datas/reward

## Files
| file | class | role |
|------|-------|------|
| `reward_group.csv` | `RewardGroup` | Reward bundle definitions |
| `reward_item.csv` | `RewardItem` | Items granted by a reward group |
| `reward_source.csv` | `RewardSource` | Claimable reward sources and claim policies |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `RewardItem.reward_type` | column | `STAMINA_UNLIMITED`, future `LIFE`, `SOFT`, `ITEM`, etc. |
| `RewardItem.stack_policy` | column | Unlimited reward stack policy, currently `EXTEND` |
| `RewardSource.source_id` | column | Client/server stable claim identifier |
| `RewardSource.claim_policy` | column | Source claim limit policy |

## Rules
- Rewards by reference only: features point to `reward_group_id`; do not inline reward columns into feature tables.
- Stamina unlimited is a reward type, not a stamina-only endpoint.

## Cross-refs
- Consumed by: `Server.RewardService`
- Consumed by: `Client.HomeTab`
