using Microsoft.EntityFrameworkCore;
using ProjectFlood.Application.Common;
using ProjectFlood.Application.Logging;
using ProjectFlood.Application.Rewards;
using ProjectFlood.Contracts.Ad;
using ProjectFlood.Contracts.Currency;
using ProjectFlood.Contracts.Rewards;
using ProjectFlood.Generated.Data;
using ProjectFlood.Infrastructure.Generated;
using StackExchange.Redis;

namespace ProjectFlood.Application.Stage;

public sealed class AdDoubleRewardService
{
    private readonly AppDbContext _db;
    private readonly IDatabase _redis;
    private readonly IAdRewardVerifier _adVerifier;
    private readonly RewardService _rewards;
    private readonly Lazy<IReadOnlyDictionary<int, ProjectFlood.Generated.Data.Stage>> _stageData;

    public AdDoubleRewardService(AppDbContext db, IConnectionMultiplexer redis, IAdRewardVerifier adVerifier, RewardService rewards)
    {
        _db = db;
        _redis = redis.GetDatabase();
        _adVerifier = adVerifier;
        _rewards = rewards;
        _stageData = new Lazy<IReadOnlyDictionary<int, ProjectFlood.Generated.Data.Stage>>(() =>
            StageLoader.LoadAsDict(System.IO.Path.Combine(AppContext.BaseDirectory, "generated", "data", "stage", "stage.csv")));
    }

    public async Task<AdDoubleRewardGrantResponse> ClaimAsync(long userId, AdDoubleRewardRequest request, string correlationId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var eligibleKey = StageAttemptService.DoubleRewardEligibleKey(userId, request.StageId);
        var storedAttemptId = await _redis.StringGetAsync(eligibleKey);

        if (!storedAttemptId.HasValue || storedAttemptId.ToString() != request.AttemptId)
            throw new GameApiException(ErrorCodes.DoubleRewardNotEligible, "Double reward not eligible or expired.");

        var result = await _adVerifier.VerifyAsync(request.Provider, request.AdToken, ct);
        if (!result.Verified)
            throw new GameApiException(ErrorCodes.AdSsvPending, "Ad SSV callback not yet received.");

        var existing = await _db.AdRewardTransactions.Query()
            .FirstOrDefaultAsync(x => x.Provider == request.Provider && x.ProviderTxId == result.ProviderTxId, ct);
        if (existing is not null)
        {
            return new AdDoubleRewardGrantResponse
            {
                Granted = false,
                Duplicate = true,
                InterstitialSuppressed = false,
                Rewards = new List<GrantedRewardDto>(),
                ServerTime = now,
            };
        }

        if (!_stageData.Value.TryGetValue(request.StageId, out var stageRow) || stageRow.reward_group_id <= 0)
            throw new GameApiException(ErrorCodes.DoubleRewardNotEligible, "Stage has no rewards.");

        // Grant reward twice (2x)
        var (items1, currency1) = await _rewards.GrantRewardGroupAsync(userId, stageRow.reward_group_id, 1, correlationId, ct);
        var (items2, currency2) = await _rewards.GrantRewardGroupAsync(userId, stageRow.reward_group_id, 1, correlationId, ct);

        var mergedRewards = items1.Select((item, i) => new GrantedRewardDto
        {
            RewardType = item.RewardType,
            TargetId = item.TargetId,
            Amount = item.Amount + (i < items2.Count ? items2[i].Amount : 0),
            DurationSeconds = item.DurationSeconds + (i < items2.Count ? items2[i].DurationSeconds : 0),
        }).ToList();

        var tx = new AdRewardTransactionsRow
        {
            Id = Guid.NewGuid().ToString("N"),
            UserId = userId,
            PlacementId = "DOUBLE_REWARD_STAGE_CLEAR",
            RewardType = "SOFT_CURRENCY",
            RewardValue = mergedRewards.Sum(r => r.Amount),
            ContextType = "stage_clear",
            ContextId = request.AttemptId,
            Provider = request.Provider,
            ProviderTxId = result.ProviderTxId,
            Status = "granted",
            CorrelationId = correlationId,
            CreatedAt = now,
            VerifiedAt = now,
            GrantedAt = now,
        };
        _db.AdRewardTransactions.Insert(tx);
        _db.EventLogs.Insert(EventLogFactory.AdDoubleRewardGranted(userId, correlationId, tx.Id, request.StageId, request.AttemptId));

        await _redis.KeyDeleteAsync(eligibleKey);
        await _db.SaveAsync(ct);

        return new AdDoubleRewardGrantResponse
        {
            Granted = true,
            Duplicate = false,
            InterstitialSuppressed = true,
            Rewards = mergedRewards,
            Currency = currency2 ?? currency1,
            ServerTime = now,
        };
    }
}
