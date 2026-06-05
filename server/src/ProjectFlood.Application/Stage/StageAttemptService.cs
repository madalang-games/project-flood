using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ProjectFlood.Application.Common;
using ProjectFlood.Application.Logging;
using ProjectFlood.Application.Ranking;
using ProjectFlood.Application.Rewards;
using ProjectFlood.Application.Stamina;
using ProjectFlood.Contracts.Rewards;
using ProjectFlood.Contracts.Stage;
using ProjectFlood.Contracts.Stamina;
using ProjectFlood.Generated.Data;
using ProjectFlood.Infrastructure.Generated;
using StackExchange.Redis;

namespace ProjectFlood.Application.Stage;

public sealed class StageAttemptService
{
    private static readonly TimeSpan DoubleRewardEligibleTtl = TimeSpan.FromMinutes(5);

    private readonly AppDbContext _db;
    private readonly IDatabase _redis;
    private readonly StaminaService _stamina;
    private readonly StaminaConfigProvider _config;
    private readonly IAdRewardVerifier _adVerifier;
    private readonly RewardService _rewards;
    private readonly RankingService _ranking;
    private readonly Lazy<IReadOnlyDictionary<int, ProjectFlood.Generated.Data.Stage>> _stageData;

    public StageAttemptService(AppDbContext db, IConnectionMultiplexer redis, StaminaService stamina, StaminaConfigProvider config, IAdRewardVerifier adVerifier, RewardService rewards, RankingService ranking)
    {
        _db = db;
        _redis = redis.GetDatabase();
        _stamina = stamina;
        _config = config;
        _adVerifier = adVerifier;
        _rewards = rewards;
        _ranking = ranking;
        _stageData = new Lazy<IReadOnlyDictionary<int, ProjectFlood.Generated.Data.Stage>>(() =>
            StageLoader.LoadAsDict(Path.Combine(AppContext.BaseDirectory, "generated", "data", "stage", "stage.csv")));
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

    public async Task<StageAttemptEndResponse> ClearAsync(long userId, int stageId, string attemptId, StageAttemptClearRequest request, string correlationId, CancellationToken ct)
    {
        var attempt = await RequireAttemptAsync(userId, stageId, attemptId);
        var now = DateTimeOffset.UtcNow;
        if (attempt.ExpiresAt <= now)
            throw new GameApiException(ErrorCodes.StageAttemptExpired, "Stage attempt expired.");

        var stageRow = _stageData.Value.TryGetValue(stageId, out var row)
            ? row
            : throw new GameApiException(ErrorCodes.StageNotFound, "Stage not found.");
        var evaluation = EvaluateClear(stageRow, request);

        await _redis.KeyDeleteAsync(UserAttemptKey(userId));

        var granted = new List<GrantedRewardDto>();
        Contracts.Currency.CurrencySnapshot? currency = null;
        StaminaSnapshot stamina;
        StageClearRankingResult rankingResult;

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        stamina = attempt.LifeSpent
            ? await _stamina.RefundAttemptLifeAsync(userId, correlationId, ct)
            : (await _stamina.GetAsync(userId, ct)).Stamina;

        if (stageRow.reward_group_id > 0)
        {
            var (items, cur) = await _rewards.GrantRewardGroupAsync(userId, stageRow.reward_group_id, 1, correlationId, ct);
            granted = items;
            currency = cur;
            _db.EventLogs.Insert(EventLogFactory.StageClearRewardGranted(userId, correlationId, stageId, stageRow.reward_group_id));
            await _redis.StringSetAsync(DoubleRewardEligibleKey(userId, stageId), attemptId, DoubleRewardEligibleTtl);
        }

        rankingResult = await _ranking.RecordClearAsync(userId, stageId, attempt.AttemptId, request, evaluation, correlationId, now, ct);
        var stageRank = await _ranking.GetStageRankAsync(stageId, rankingResult.BestTurnsUsed, ct);
        _db.EventLogs.Insert(EventLogFactory.StageAttemptCleared(userId, correlationId, attempt.AttemptId, stageId, attempt.LifeSpent));
        await _db.SaveAsync(ct);
        await tx.CommitAsync(ct);
        _ranking.QueueRedisUpdate(userId, stageId, rankingResult);

        return new StageAttemptEndResponse
        {
            AttemptId = attempt.AttemptId,
            StageId = stageId,
            Result = "CLEAR",
            LifeRefunded = attempt.LifeSpent,
            Stars = rankingResult.Stars,
            TurnsUsed = rankingResult.TurnsUsed,
            StageRank = stageRank,
            IsNewBest = rankingResult.IsNewBest,
            GrantedRewards = granted,
            Currency = currency,
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
        string adToken,
        string correlationId,
        CancellationToken ct)
    {
        var attempt = await RequireAttemptAsync(userId, stageId, attemptId);
        var now = DateTimeOffset.UtcNow;
        if (attempt.ExpiresAt <= now)
            throw new GameApiException(ErrorCodes.StageAttemptExpired, "Stage attempt expired.");

        var config = _config.Current;
        if (attempt.ReviveCount >= config.MaxRevivePerAttempt)
            throw new GameApiException(ErrorCodes.ReviveLimitExceeded, "Revive limit exceeded.");

        var result = await _adVerifier.VerifyAsync(provider, adToken, ct);
        if (!result.Verified)
            throw new GameApiException(ErrorCodes.AdSsvPending, "Ad SSV callback not yet received.");

        var existing = await _db.AdRewardTransactions.Query()
            .FirstOrDefaultAsync(x => x.Provider == provider && x.ProviderTxId == result.ProviderTxId, ct);
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
            ProviderTxId = result.ProviderTxId,
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

    private static StageClearEvaluation EvaluateClear(ProjectFlood.Generated.Data.Stage stage, StageAttemptClearRequest request)
    {
        if (request.RulesetVersion != stage.ruleset_version)
            throw new GameApiException(ErrorCodes.StageRulesetMismatch, "Stage ruleset mismatch.");
        if (request.TurnsUsed < 0 || request.TurnsUsed > stage.turn_limit)
            throw new GameApiException(ErrorCodes.InvalidStageClear, "Invalid turns used.");
        if (request.CoreRemaining)
            throw new GameApiException(ErrorCodes.InvalidStageClear, "Core cell remains.");

        var totalBasicCells = CountBasicCells(stage);
        if (totalBasicCells <= 0)
            throw new GameApiException(ErrorCodes.InvalidStageClear, "Stage has no basic cells.");
        if (request.RemainingBasicCells < 0 || request.RemainingBasicCells > totalBasicCells)
            throw new GameApiException(ErrorCodes.InvalidStageClear, "Invalid remaining basic cells.");

        var cleared = totalBasicCells - request.RemainingBasicCells;
        var ratio = (float)cleared / totalBasicCells;
        var stars = request.RemainingBasicCells == 0
            ? 3
            : ratio >= stage.star2_ratio
                ? 2
                : ratio >= stage.star1_ratio
                    ? 1
                    : 0;

        if (stars <= 0)
            throw new GameApiException(ErrorCodes.InvalidStageClear, "Clear ratio does not satisfy minimum star threshold.");

        return new StageClearEvaluation(stars, totalBasicCells);
    }

    private static int CountBasicCells(ProjectFlood.Generated.Data.Stage stage)
    {
        var expectedLength = stage.board_width * stage.board_height * 3;
        if (stage.cells.Length != expectedLength)
            throw new GameApiException(ErrorCodes.InvalidStageClear, "Stage cells length is invalid.");

        var count = 0;
        for (var i = 0; i < stage.cells.Length; i += 3)
        {
            if (stage.cells[i + 1] == '0')
                count++;
        }

        return count;
    }

    private static string UserAttemptKey(long userId) => $"stage_attempt:user:{userId}";
    public static string DoubleRewardEligibleKey(long userId, int stageId) => $"double_reward_eligible:{userId}:{stageId}";

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
