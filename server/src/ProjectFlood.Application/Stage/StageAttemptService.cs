using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ProjectFlood.Application.Common;
using ProjectFlood.Application.Logging;
using ProjectFlood.Application.Rewards;
using ProjectFlood.Application.Stamina;
using ProjectFlood.Contracts.Stage;
using ProjectFlood.Infrastructure.Generated;
using StackExchange.Redis;

namespace ProjectFlood.Application.Stage;

public sealed class StageAttemptService
{
    private readonly AppDbContext _db;
    private readonly IDatabase _redis;
    private readonly StaminaService _stamina;
    private readonly StaminaConfigProvider _config;
    private readonly IAdRewardVerifier _adVerifier;

    public StageAttemptService(AppDbContext db, IConnectionMultiplexer redis, StaminaService stamina, StaminaConfigProvider config, IAdRewardVerifier adVerifier)
    {
        _db = db;
        _redis = redis.GetDatabase();
        _stamina = stamina;
        _config = config;
        _adVerifier = adVerifier;
    }

    public async Task<StageAttemptStartResponse> StartAsync(long userId, int stageId, string correlationId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var existing = await GetAttemptAsync(userId);
        if (existing is not null)
            _db.EventLogs.Insert(EventLogFactory.StageAttemptReplaced(userId, correlationId, existing.AttemptId, existing.StageId));

        var (snapshot, lifeSpent) = await _stamina.SpendForAttemptAsync(userId, correlationId, ct);
        var timeoutSeconds = _config.Current.AttemptTimeoutSeconds;
        var attempt = new StageAttemptState
        {
            AttemptId = Guid.NewGuid().ToString("N"),
            UserId = userId,
            StageId = stageId,
            StartedAt = now,
            ExpiresAt = now.AddSeconds(timeoutSeconds),
            LifeSpent = lifeSpent,
            ReviveCount = 0,
        };

        await _redis.StringSetAsync(UserAttemptKey(userId), JsonSerializer.Serialize(attempt), TimeSpan.FromSeconds(timeoutSeconds));
        _db.EventLogs.Insert(EventLogFactory.StageAttemptStarted(userId, correlationId, attempt.AttemptId, stageId, lifeSpent, attempt.ExpiresAt));
        await _db.SaveAsync(ct);

        return new StageAttemptStartResponse
        {
            Attempt = ToSnapshot(attempt),
            Stamina = snapshot,
            ServerTime = now,
        };
    }

    public async Task<StageAttemptEndResponse> ClearAsync(long userId, int stageId, string attemptId, string correlationId, CancellationToken ct)
    {
        var attempt = await RequireAttemptAsync(userId, stageId, attemptId);
        var now = DateTimeOffset.UtcNow;
        if (attempt.ExpiresAt <= now)
            throw new GameApiException(ErrorCodes.StageAttemptExpired, "Stage attempt expired.");

        await _redis.KeyDeleteAsync(UserAttemptKey(userId));
        var stamina = attempt.LifeSpent
            ? await _stamina.RefundAttemptLifeAsync(userId, correlationId, ct)
            : (await _stamina.GetAsync(userId, ct)).Stamina;

        _db.EventLogs.Insert(EventLogFactory.StageAttemptCleared(userId, correlationId, attempt.AttemptId, stageId, attempt.LifeSpent));
        await _db.SaveAsync(ct);

        return new StageAttemptEndResponse
        {
            AttemptId = attempt.AttemptId,
            StageId = stageId,
            Result = "CLEAR",
            LifeRefunded = attempt.LifeSpent,
            Stamina = stamina,
            ServerTime = now,
        };
    }

    public async Task<StageAttemptEndResponse> FailAsync(long userId, int stageId, string attemptId, string reason, string correlationId, CancellationToken ct)
    {
        var attempt = await RequireAttemptAsync(userId, stageId, attemptId);
        var now = DateTimeOffset.UtcNow;
        await _redis.KeyDeleteAsync(UserAttemptKey(userId));
        _db.EventLogs.Insert(EventLogFactory.StageAttemptFailed(userId, correlationId, attempt.AttemptId, stageId, string.IsNullOrWhiteSpace(reason) ? "fail" : reason));
        await _db.SaveAsync(ct);
        var stamina = (await _stamina.GetAsync(userId, ct)).Stamina;

        return new StageAttemptEndResponse
        {
            AttemptId = attempt.AttemptId,
            StageId = stageId,
            Result = "FAIL",
            LifeRefunded = false,
            Stamina = stamina,
            ServerTime = now,
        };
    }

    public async Task<StageReviveAdResponse> ReviveAdAsync(
        long userId,
        int stageId,
        string attemptId,
        string provider,
        string providerTransactionId,
        string adToken,
        string correlationId,
        CancellationToken ct)
    {
        var attempt = await RequireAttemptAsync(userId, stageId, attemptId);
        var now = DateTimeOffset.UtcNow;
        if (attempt.ExpiresAt <= now)
            throw new GameApiException(ErrorCodes.StageAttemptExpired, "Stage attempt expired.");

        var existing = await _db.AdRewardTransactions.Query()
            .FirstOrDefaultAsync(x => x.Provider == provider && x.ProviderTxId == providerTransactionId, ct);
        if (existing is not null)
        {
            if (existing.UserId != userId || existing.ContextType != "stage_attempt" || existing.ContextId != attemptId)
                throw new GameApiException(ErrorCodes.AdRewardDuplicate, "Ad reward transaction is already owned by another context.");

            return new StageReviveAdResponse
            {
                Granted = false,
                Duplicate = true,
                ReviveCount = attempt.ReviveCount,
                TurnsGranted = 0,
                Attempt = ToSnapshot(attempt),
                ServerTime = now,
            };
        }

        var config = _config.Current;
        if (attempt.ReviveCount >= config.MaxRevivePerAttempt)
            throw new GameApiException(ErrorCodes.ReviveLimitExceeded, "Revive limit exceeded.");
        if (!await _adVerifier.VerifyAsync(provider, providerTransactionId, adToken, ct))
            throw new GameApiException(ErrorCodes.AdRewardVerifyFailed, "Ad reward verification failed.");

        attempt.ReviveCount++;
        var turnsGranted = config.ReviveTurns[attempt.ReviveCount - 1];
        await _redis.StringSetAsync(UserAttemptKey(userId), JsonSerializer.Serialize(attempt), attempt.ExpiresAt - now);

        var tx = new AdRewardTransactionsRow
        {
            Id = Guid.NewGuid().ToString("N"),
            UserId = userId,
            PlacementId = "STAGE_REVIVE",
            RewardType = "REVIVE_TURN",
            RewardValue = turnsGranted,
            ContextType = "stage_attempt",
            ContextId = attemptId,
            Provider = provider,
            ProviderTxId = providerTransactionId,
            Status = "granted",
            CorrelationId = correlationId,
            CreatedAt = now,
            VerifiedAt = now,
            GrantedAt = now,
        };
        _db.AdRewardTransactions.Insert(tx);
        _db.EventLogs.Insert(EventLogFactory.AdRewardClaimed(userId, correlationId, tx.Id, tx.PlacementId, tx.RewardType, tx.RewardValue, false));
        _db.EventLogs.Insert(EventLogFactory.StageAttemptRevivedByAd(userId, correlationId, attempt.AttemptId, stageId, attempt.ReviveCount, turnsGranted, tx.Id));
        await _db.SaveAsync(ct);

        return new StageReviveAdResponse
        {
            Granted = true,
            Duplicate = false,
            ReviveCount = attempt.ReviveCount,
            TurnsGranted = turnsGranted,
            Attempt = ToSnapshot(attempt),
            ServerTime = now,
        };
    }

    private async Task<StageAttemptState?> GetAttemptAsync(long userId)
    {
        var value = await _redis.StringGetAsync(UserAttemptKey(userId));
        return value.HasValue ? JsonSerializer.Deserialize<StageAttemptState>(value!) : null;
    }

    private async Task<StageAttemptState> RequireAttemptAsync(long userId, int stageId, string attemptId)
    {
        var attempt = await GetAttemptAsync(userId);
        if (attempt is null || attempt.AttemptId != attemptId || attempt.StageId != stageId)
            throw new GameApiException(ErrorCodes.InvalidStageAttempt, "Invalid stage attempt.");
        return attempt;
    }

    private StageAttemptSnapshot ToSnapshot(StageAttemptState attempt)
        => new()
        {
            AttemptId = attempt.AttemptId,
            StageId = attempt.StageId,
            ExpiresAt = attempt.ExpiresAt,
            ReviveCount = attempt.ReviveCount,
            RemainingRevives = Math.Max(0, _config.Current.MaxRevivePerAttempt - attempt.ReviveCount),
            LifeSpent = attempt.LifeSpent,
        };

    private static string UserAttemptKey(long userId) => $"stage_attempt:user:{userId}";

    private sealed class StageAttemptState
    {
        public string AttemptId { get; set; } = string.Empty;
        public long UserId { get; set; }
        public int StageId { get; set; }
        public DateTimeOffset StartedAt { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
        public bool LifeSpent { get; set; }
        public int ReviveCount { get; set; }
    }
}
