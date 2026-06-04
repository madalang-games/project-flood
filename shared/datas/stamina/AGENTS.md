# shared/datas/stamina

## Files
| file | class | role |
|------|-------|------|
| `stamina_config.csv` | `StaminaConfig` | Life, recovery, attempt timeout, revive ad, and daily reset config |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `StaminaConfig.max_life` | column | Maximum life cap |
| `StaminaConfig.regen_seconds` | column | Natural recovery interval per life |
| `StaminaConfig.attempt_timeout_seconds` | column | Redis attempt TTL source |
| `StaminaConfig.max_revive_per_attempt` | column | Revive ad cap per attempt |
| `StaminaConfig.default_unlimited_stack_policy` | column | Default stack behavior for unlimited stamina rewards |

## Rules
- Attempt timeout and stamina tuning must come from this table; do not hardcode server TTLs.
- Keep stamina-specific rules here; generic rewards live in `shared/datas/reward/`.

## Cross-refs
- Consumed by: `Server.StaminaService`
- Consumed by: `Server.StageAttemptService`
- Consumed by: `Client.HomeTab`
