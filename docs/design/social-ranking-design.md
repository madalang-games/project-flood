# Social & Ranking System Design

## 1. Authority
- DB is the source of truth for player progress and ranking aggregates.
- Redis is a rebuildable ranking cache/index. Redis writes happen after DB commit and may be dirty-read by clear responses.
- Redis loss is recovered from DB during server init, lazy rebuild on cache miss, or admin-triggered rebuild.
- Stage clear validation uses server-side static data fields marked `CS` in `shared/datas/stage/stage.csv`.

## 2. Stage Clear Validation
Client sends summary inputs in `StageAttemptClearRequest`:
- `ruleset_version`
- `turns_used`
- `remaining_basic_cells`
- `core_remaining`

Server validates:
- active Redis attempt exists and matches user/stage/attempt id
- attempt is not expired
- request ruleset matches static stage ruleset
- `turns_used` is within `0..turn_limit`
- `remaining_basic_cells` is within `0..total_basic_cells`
- `core_remaining == false`

Server computes stars from static stage data:
- `total_basic_cells` is parsed from stage `cells`, excluding obstacle and void cells
- cleared ratio = `(total_basic_cells - remaining_basic_cells) / total_basic_cells`
- 3 stars if all basic cells were cleared
- 2 stars if ratio is at least `star2_ratio`
- 1 star if ratio is at least `star1_ratio`
- otherwise the clear request is invalid

## 3. DB Model
`players`
- Stores player accounts.
- `display_name` (string(64)): Player's custom public name.
- `avatar_id` (int32): Reference to static metadata avatar image (default to 1). Users do not upload custom images; avatars are selected from pre-defined IDs.

`user_stage_progress`
- One row per user/stage.
- Stores `best_star`, `best_turns_used`, first clear time, best update times.
- Used to rebuild stage ranking Redis keys.

`stage_clear_records`
- Append-only clear audit.
- Stores the validation inputs, computed total basic cells, stars, and `is_new_best`.

`user_ranking_totals`
- One row per user.
- Stores `total_earned_stars`, timestamp when the current total was achieved, `max_cleared_stage_id`, and timestamp when that max was achieved.
- Stores `win_streak`: Current consecutive stage clears. Resets to 0 upon stage failure or abandonment.
- Stores `max_win_streak`: All-time highest consecutive stage clears. Does not decrease when a streak is broken. Updated only when `win_streak` exceeds `max_win_streak` on clear.
- Used to rebuild global ranking Redis keys.

## 4. Stage Ranking
- Ranking unit: per-stage best `turns_used`.
- Only each player's best score is indexed; duplicate lower-quality clears do not create extra ranking entries.
- Ranking style: competition ranking.
- My stage rank = number of players with strictly lower best turns + 1.
- Stage ranking API only exposes my rank and my best turns for the requested stage.

Redis key:
- `ranking:stage:{stageId}:turns`
- member: `user_id`
- score: `best_turns_used`

## 5. Global Ranking
Ranking tab exposes two paged tabs.

Stars tab:
- score: `total_earned_stars`
- higher is better
- tie-breaker: earlier `total_stars_achieved_at`

Max stage tab:
- score: `max_cleared_stage_id`
- higher is better
- tie-breaker: earlier `max_stage_achieved_at`

Redis keys use an ascending numeric composite score:
- `ranking:global:stars`
- `ranking:global:max_stage`
- score = `-primaryScore * 1_000_000_000 + achievedUnixSeconds`

Because the tie-breaker is deterministic, global list ranks are dense by sorted position. A separate `my rank` endpoint returns the current user's card for the ranking tab footer.

## 6. API Contract
Stage clear response adds:
- `stars`
- `turns_used`
- `stage_rank`
- `is_new_best`

`is_new_best` is true only when `turns_used` improves the player's per-stage best turns.

Ranking API:
- `GET /api/rankings/global/stars?offset=&limit=`
- `GET /api/rankings/global/stars/me`
- `GET /api/rankings/global/max-stage?offset=&limit=`
- `GET /api/rankings/global/max-stage/me`
- `GET /api/rankings/stages/{stageId}/me`
- `POST /api/rankings/admin/rebuild`

Profile API:
- `POST /api/player/profile`
  - Request: `display_name` (optional), `avatar_id` (optional)
  - Validates avatar unlock conditions using `avatar.csv`.
  - Response: Updated `user_id`, `display_name`, `avatar_id`.

Page size is clamped server-side. Clients should use paging instead of requesting the full leaderboard.

## 7. Avatar Metadata (`avatar.csv`)
Avatars are defined in `shared/datas/avatar/avatar.csv` static metadata. Players cannot upload custom images, they must select from predefined options.

Fields:
- `avatar_id`: Unique integer key.
- `resource_name`: Unity sprite reference name.
- `unlock_cost`: Soft currency (gold) required to unlock (0 if free).
- `unlock_type`: Condition category (`free`, `gold`, `achievement`).
