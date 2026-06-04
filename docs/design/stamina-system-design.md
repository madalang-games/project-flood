# Stamina System Design

## Goals
- Hyper-casual friendly soft gate: users may keep playing by watching rewarded ads.
- Server is the source of truth for life, unlimited time, reward claim limits, and ad reward transactions.
- Stage attempt validation is fast and volatile: Redis owns active attempt state, DB owns only durable user state and audit logs.

## Player Rules
| rule | value |
|------|-------|
| Max life | 5 |
| Natural recovery | 1 life per 600 seconds |
| Life ad reward | Unlimited daily claims, no cooldown, +1 life, blocked at max life |
| Life spend | Attempt start consumes 1 life unless unlimited is active |
| Life refund | Clear refunds the spent life, capped by max life |
| Failure/give-up/timeout | Spent life remains consumed |
| Early exemption | None |
| Unlimited time | Real server time, not play time |
| Daily unlimited reset | KST 00:00 |
| Daily unlimited source | Reward source `DAILY_STAMINA_UNLIMITED` |
| Unlimited stack policy | `EXTEND` |
| Natural recovery during unlimited | Continues |
| No-ads purchase | Removes forced ads only; rewarded ads remain available |

## Stage Attempt Rules
| rule | value |
|------|-------|
| Active attempt limit | 1 per user |
| Attempt storage | Redis TTL |
| Attempt ID | Server-generated |
| Timeout | Config-driven, no hardcoded TTL |
| New attempt while active | Existing attempt is discarded without refund and logged as `replaced_by_new_attempt` |
| Redis loss | User loss is accepted; invalid/expired attempt errors are returned |
| Clear/fail request | Must include `attempt_id` |
| Final states | Final state is immutable |

## Revive Ads
| revive count | turns granted |
|--------------|---------------|
| 1 | 3 |
| 2 | 2 |
| 3 | 1 |

- Revive count increments only after successful ad reward verification.
- Attempts reject revive claims after the configured max revive count.
- Revive ad rewards are tied to `context_type=stage_attempt` and `context_id=attempt_id`.

## Reward Model
Stamina unlimited is a generic reward type, not a dedicated daily-stamina endpoint. The HomeTab badge displays a claimable reward source and calls the common reward claim API.

| concept | role |
|---------|------|
| Reward group | Bundle of reward items |
| Reward item | Actual granted thing, e.g. `STAMINA_UNLIMITED` |
| Reward source | Why/how a user can claim a group, e.g. daily free HomeTab badge |
| Claim state | Per-user source claim limit state |

## Ad Reward Model
Ad reward transactions are shared across stamina and future ad rewards.

| behavior | result |
|----------|--------|
| First valid claim | Grant reward and return `granted=true`, positive delta |
| Duplicate same user/context tx | Return `granted=false`, `duplicate=true`, `delta=0`, current snapshot |
| Duplicate tx with different user/context | Reject with `AD_REWARD_DUPLICATE` |
| Full life claim | Client dims first; server rejects with `STAMINA_FULL` |

## API Surface
| method | route | purpose |
|--------|-------|---------|
| GET | `/api/stamina` | Current stamina snapshot |
| POST | `/api/stages/{stageId}/attempts/start` | Start attempt and maybe spend life |
| POST | `/api/stages/{stageId}/attempts/{attemptId}/clear` | Clear attempt and maybe refund life |
| POST | `/api/stages/{stageId}/attempts/{attemptId}/fail` | Fail/give-up attempt |
| POST | `/api/stages/{stageId}/attempts/{attemptId}/revive-ad` | Claim revive rewarded ad |
| GET | `/api/rewards/sources` | List claimable reward sources |
| POST | `/api/rewards/claim` | Claim non-ad reward source |
| POST | `/api/ad-rewards/claim` | Claim rewarded-ad reward source |

## Error Codes
| code | meaning |
|------|---------|
| `INSUFFICIENT_STAMINA` | No life and no unlimited time |
| `STAMINA_FULL` | Life ad reward requested while at max life |
| `INVALID_STAGE_ATTEMPT` | Attempt is missing, mismatched, or no longer active |
| `STAGE_ATTEMPT_EXPIRED` | Attempt exceeded configured TTL |
| `REVIVE_LIMIT_EXCEEDED` | Attempt already used all revives |
| `REWARD_ALREADY_CLAIMED` | Source claim limit reached for the current period |
| `AD_REWARD_DUPLICATE` | Provider transaction already belongs to another user/context |
| `AD_REWARD_VERIFY_FAILED` | Provider verification failed |

## Event Logs
All modifying actions append to `event_logs` with a request `correlation_id`.

| event | required params |
|-------|-----------------|
| `StaminaLifeChanged` | `delta`, `reason`, `current_after` |
| `StaminaUnlimitedChanged` | `source_id`, `duration_seconds`, `unlimited_until_utc` |
| `StageAttemptStarted` | `attempt_id`, `stage_id`, `life_spent`, `expires_at_utc` |
| `StageAttemptCleared` | `attempt_id`, `stage_id`, `life_refunded` |
| `StageAttemptFailed` | `attempt_id`, `stage_id`, `reason` |
| `StageAttemptReplaced` | `attempt_id`, `stage_id`, `reason` |
| `StageAttemptRevivedByAd` | `attempt_id`, `stage_id`, `revive_count`, `turns_granted`, `ad_tx_id` |
| `RewardClaimed` | `source_id`, `reward_group_id` |
| `AdRewardClaimed` | `ad_tx_id`, `placement_id`, `reward_type`, `reward_value`, `duplicate` |

## Open Operating Notes
- Redis active attempt is intentionally not durable. CS can inspect `event_logs` and ad transactions, but cannot reconstruct lost Redis state perfectly.
- Stamina and reward claim APIs should return full snapshots after mutation so the client can replace local state instead of applying blind deltas.
