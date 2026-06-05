using System.Text.Json;
using ProjectFlood.Domain.Logging;
using ProjectFlood.Infrastructure.Generated;

namespace ProjectFlood.Application.Logging;

public static class EventLogFactory
{
    public static EventLogsRow Create(long userId, int trId, string correlationId, object parameters)
        => new()
        {
            UserId = userId,
            TrId = trId,
            CorrelationId = correlationId,
            Params = JsonSerializer.Serialize(parameters),
            CreatedAt = DateTimeOffset.UtcNow,
        };

    public static EventLogsRow StaminaLifeChanged(long userId, string correlationId, int delta, string reason, int currentAfter)
        => Create(userId, EventLogIds.StaminaLifeChanged, correlationId, new { delta, reason, current_after = currentAfter });

    public static EventLogsRow StaminaUnlimitedChanged(long userId, string correlationId, string sourceId, int durationSeconds, DateTimeOffset unlimitedUntil)
        => Create(userId, EventLogIds.StaminaUnlimitedChanged, correlationId, new { source_id = sourceId, duration_seconds = durationSeconds, unlimited_until_utc = unlimitedUntil });

    public static EventLogsRow StageAttemptStarted(long userId, string correlationId, string attemptId, int stageId, bool lifeSpent, DateTimeOffset expiresAt)
        => Create(userId, EventLogIds.StageAttemptStarted, correlationId, new { attempt_id = attemptId, stage_id = stageId, life_spent = lifeSpent, expires_at_utc = expiresAt });

    public static EventLogsRow StageAttemptCleared(long userId, string correlationId, string attemptId, int stageId, bool lifeRefunded)
        => Create(userId, EventLogIds.StageAttemptCleared, correlationId, new { attempt_id = attemptId, stage_id = stageId, life_refunded = lifeRefunded });

    public static EventLogsRow StageAttemptFailed(long userId, string correlationId, string attemptId, int stageId, string reason)
        => Create(userId, EventLogIds.StageAttemptFailed, correlationId, new { attempt_id = attemptId, stage_id = stageId, reason });

    public static EventLogsRow StageAttemptReplaced(long userId, string correlationId, string attemptId, int stageId)
        => Create(userId, EventLogIds.StageAttemptReplaced, correlationId, new { attempt_id = attemptId, stage_id = stageId, reason = "replaced_by_new_attempt" });

    public static EventLogsRow StageAttemptRevivedByAd(long userId, string correlationId, string attemptId, int stageId, int reviveCount, int turnsGranted, string adTxId)
        => Create(userId, EventLogIds.StageAttemptRevivedByAd, correlationId, new { attempt_id = attemptId, stage_id = stageId, revive_count = reviveCount, turns_granted = turnsGranted, ad_tx_id = adTxId });

    public static EventLogsRow RewardClaimed(long userId, string correlationId, string sourceId, int rewardGroupId)
        => Create(userId, EventLogIds.RewardClaimed, correlationId, new { source_id = sourceId, reward_group_id = rewardGroupId });

    public static EventLogsRow AdRewardClaimed(long userId, string correlationId, string adTxId, string placementId, string rewardType, int rewardValue, bool duplicate)
        => Create(userId, EventLogIds.AdRewardClaimed, correlationId, new { ad_tx_id = adTxId, placement_id = placementId, reward_type = rewardType, reward_value = rewardValue, duplicate });

    public static EventLogsRow StageClearRewardGranted(long userId, string correlationId, int stageId, int rewardGroupId)
        => Create(userId, EventLogIds.StageClearRewardGranted, correlationId, new { stage_id = stageId, reward_group_id = rewardGroupId });

    public static EventLogsRow CurrencyChanged(long userId, string correlationId, long delta, string reason, long amountAfter)
        => Create(userId, EventLogIds.CurrencyChanged, correlationId, new { delta, reason, amount_after = amountAfter });

    public static EventLogsRow AdInterstitialShown(long userId, string correlationId, int stageId)
        => Create(userId, EventLogIds.AdInterstitialShown, correlationId, new { stage_id = stageId });

    public static EventLogsRow AdDoubleRewardGranted(long userId, string correlationId, string adTxId, int stageId, string attemptId)
        => Create(userId, EventLogIds.AdDoubleRewardGranted, correlationId, new { ad_tx_id = adTxId, stage_id = stageId, attempt_id = attemptId });
}
